using System.Security.Cryptography;
using BuildingBlocks.Data;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Application.Contracts.Keycloak;
using UserService.Application.Events.Users;
using UserService.Domain.Aggregates;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;
using UserService.Domain.ValueObjects;
using UserService.Infrastructure.Contracts;
using UserService.Infrastructure.Dtos.Keycloak;

namespace UserService.Application.Commands.Users.Create;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<Guid>>
{
    public CreateUserCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateUserCommandHandler> logger, IKeycloakService keycloakService, IUserRepository userRepository, IPasswordEncripter passwordEncripter, IMediator mediator, IUserTokenRepository userTokenRepository)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _keycloakService = keycloakService;
        _userRepository = userRepository;
        _passwordEncripter = passwordEncripter;
        _mediator = mediator;
        _userTokenRepository = userTokenRepository;
    }


    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserCommandHandler> _logger;
    private readonly IKeycloakService _keycloakService;
    private readonly IUserRepository   _userRepository;
    private readonly IPasswordEncripter  _passwordEncripter;    
    private readonly IMediator _mediator;
    private readonly IUserTokenRepository _userTokenRepository;
    
    public async Task<ApiResponse<Guid>> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Criar usuário no Keycloak
            var keycloakUserId = await CreateUserKeycloakAsync(request);
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return ApiResponse<Guid>.Fail("KEYCLOAK_ERROR", "Falha na criação do usuário no sistema de autenticação. Tente novamente.");
            }

            // Criar usuário local
            var userId = await CreateUserAsync(request, keycloakUserId, cancellationToken);
            
            return ApiResponse<Guid>.Ok(userId);
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Dados inválidos para criação de usuário: {Email}", request.Email);
            return ApiResponse<Guid>.Fail("VALIDATION_ERROR", argEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar usuário: {Email}", request.Email);
            return ApiResponse<Guid>.Fail("INTERNAL_ERROR", 
                "Erro interno do servidor. Tente novamente.");
        }
    }

    /// <summary>
    /// Cria usuário no Keycloak e retorna o ID do usuário criado
    /// </summary>
    /// <param name="request">Dados do usuário a ser criado</param>
    /// <returns>ID do usuário no Keycloak ou null em caso de erro</returns>
    private async Task<string?> CreateUserKeycloakAsync(CreateUserCommand request)
    {
        try
        {
            _logger.LogInformation("Iniciando criação de usuário no Keycloak para email: {Email}", request.Email);
            
            // Validar se o usuário já existe no Keycloak
            var existingKeycloakUser = await _keycloakService.GetUserByEmailAsync(request.Email);
            if (existingKeycloakUser != null)
            {
                _logger.LogWarning("Tentativa de criar usuário já existente no Keycloak: {Email}", request.Email);
                throw new InvalidOperationException($"Já existe um usuário no Keycloak com o email {request.Email}");
            }

            // Criar usuário no Keycloak
            var createUserKeycloak = new CreateUserKeycloak(
                Username: request.Email,
                Email: request.Email,
                FirstName: request.FirstName,
                LastName: request.LastName,
                Password: request.Password,
                Enabled: true,
                EmailVerified: false,
                Roles: new List<string> { "user" }
            );
            
            var userKeycloakId = await _keycloakService.CreateUserAsync(createUserKeycloak);
            
            if (string.IsNullOrEmpty(userKeycloakId))
            {
                _logger.LogError("Keycloak não retornou ID do usuário: {Email}", request.Email);
                return null;
            }
            
            _logger.LogInformation("Usuário criado no Keycloak com sucesso. ID: {KeycloakUserId}", userKeycloakId);
            return userKeycloakId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário no Keycloak: {Email}", request.Email);
            throw;
        }
    }

    /// <summary>
    /// Cria usuário local, token de ativação e salva no banco de dados
    /// </summary>
    /// <param name="request">Dados do usuário</param>
    /// <param name="keycloakUserId">ID do usuário no Keycloak</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>ID do usuário criado</returns>
    private async Task<Guid> CreateUserAsync(CreateUserCommand request, string keycloakUserId, CancellationToken cancellationToken)
    {
        // Iniciar transação local
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Validar se usuário já existe localmente
            var existingUser = await _userRepository.GetByEmailAsync(Email.Create(request.Email), cancellationToken);
            if (existingUser != null)
            {
                _logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", request.Email);
                
                // Fazer rollback da transação local
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                // Fazer rollback do Keycloak
                await RollbackKeycloakUserAsync(keycloakUserId);
                
                throw new InvalidOperationException($"Já existe um usuário cadastrado com o email {request.Email}");
            }

            // Criar entidade User
            var user = User.Create(
                firstName: request.FirstName,
                lastName: request.LastName,
                email: Email.Create(request.Email),
                role: UserRole.Customer,
                keyCloakId: Guid.Parse(keycloakUserId)
            );
            
            // Configurações adicionais
            user.PasswordHash = _passwordEncripter.Encrypt(request.Password);
            user.NewsletterOptIn = request.NewsletterOptIn;
            user.Status = UserStatus.Inativo; // Usuário inativo até ativar a conta
            
            // Criar token de ativação
            var activationToken = GenerateActivationToken();
            var userToken = new UserToken
            {
                TokenId = Guid.NewGuid(),
                UserId = user.UserId,
                TokenType = UserTokenType.EmailVerification,
                TokenValue = activationToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // Token expira em 24 horas
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            // Salvar no banco de dados
            await _userRepository.AddAsync(user);
            await _userTokenRepository.AddAsync(userToken);
            await _unitOfWork.SaveChangesAsync();
            
            // Confirmar transação local
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            _logger.LogInformation("Usuário criado com sucesso. ID: {UserId}", user.UserId);
            
            // TODO: Publicar evento de usuário criado
            // await _mediator.PublishAsync(new UserCreatedEvent(user.UserId), cancellationToken);
            
            return user.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário local: {Email}", request.Email);
            
            try
            {
                // Fazer rollback da transação local
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Erro adicional ao fazer rollback da transação local para usuário: {Email}", request.Email);
            }
            
            // Fazer rollback do Keycloak em caso de erro
            await RollbackKeycloakUserAsync(keycloakUserId);
            
            throw;
        }
    }

    /// <summary>
    /// Faz rollback do usuário criado no Keycloak
    /// </summary>
    /// <param name="keycloakUserId">ID do usuário no Keycloak</param>
    private async Task RollbackKeycloakUserAsync(string keycloakUserId)
    {
        if (string.IsNullOrEmpty(keycloakUserId))
            return;

        _logger.LogWarning("Tentando fazer rollback do usuário no Keycloak: {KeycloakUserId}", keycloakUserId);
        try
        {
            await _keycloakService.DeleteUserAsync(keycloakUserId);
            _logger.LogInformation("Rollback do Keycloak realizado com sucesso: {KeycloakUserId}", keycloakUserId);
        }
        catch (Exception rollbackEx)
        {
            _logger.LogError(rollbackEx, "Falha no rollback do Keycloak para usuário: {KeycloakUserId}", keycloakUserId);
        }
    }
    
    /// <summary>
    /// Gera um token de ativação seguro
    /// </summary>
    /// <returns>Token de ativação como string</returns>
    private static string GenerateActivationToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256 bits
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", ""); // URL-safe base64
    }
}