using BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using UserService.Infrastructure.Contracts;

namespace UserService.Infrastructure.Services.Email;

/// <summary>
/// Implementação fake do serviço de email para capturar códigos de ativação durante testes
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly List<CapturedEmail> _capturedEmails;
    private readonly object _lock = new();

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _capturedEmails = new List<CapturedEmail>();
    }

    /// <summary>
    /// Simula o envio de email de ativação e captura o código
    /// </summary>
    public async Task<Result> SendAccountActivationEmailAsync(
        string email, 
        string firstName, 
        string activationToken, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure("Email é obrigatório");

            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure("Nome é obrigatório");

            if (string.IsNullOrWhiteSpace(activationToken))
                return Result.Failure("Token de ativação é obrigatório");

            var capturedEmail = new CapturedEmail
            {
                Type = EmailType.AccountActivation,
                Email = email,
                FirstName = firstName,
                Token = activationToken,
                SentAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                _capturedEmails.Add(capturedEmail);
            }

            _logger.LogInformation(
                "Email de ativação capturado para {Email}. Token: {Token}", 
                email, 
                activationToken);

            // Simula delay de envio
            await Task.Delay(100, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao capturar email de ativação para {Email}", email);
            return Result.Failure($"Erro interno: {ex.Message}");
        }
    }

    /// <summary>
    /// Simula o envio de email de boas-vindas
    /// </summary>
    public async Task<Result> SendWelcomeEmailAsync(
        string email, 
        string firstName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure("Email é obrigatório");

            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure("Nome é obrigatório");

            var capturedEmail = new CapturedEmail
            {
                Type = EmailType.Welcome,
                Email = email,
                FirstName = firstName,
                SentAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                _capturedEmails.Add(capturedEmail);
            }

            _logger.LogInformation("Email de boas-vindas capturado para {Email}", email);

            // Simula delay de envio
            await Task.Delay(100, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao capturar email de boas-vindas para {Email}", email);
            return Result.Failure($"Erro interno: {ex.Message}");
        }
    }

    /// <summary>
    /// Simula o envio de email de redefinição de senha
    /// </summary>
    public async Task<Result> SendPasswordResetEmailAsync(
        string email, 
        string firstName, 
        string resetToken, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure("Email é obrigatório");

            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure("Nome é obrigatório");

            if (string.IsNullOrWhiteSpace(resetToken))
                return Result.Failure("Token de redefinição é obrigatório");

            var capturedEmail = new CapturedEmail
            {
                Type = EmailType.PasswordReset,
                Email = email,
                FirstName = firstName,
                Token = resetToken,
                SentAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                _capturedEmails.Add(capturedEmail);
            }

            _logger.LogInformation(
                "Email de redefinição de senha capturado para {Email}. Token: {Token}", 
                email, 
                resetToken);

            // Simula delay de envio
            await Task.Delay(100, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao capturar email de redefinição de senha para {Email}", email);
            return Result.Failure($"Erro interno: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém o último código de ativação capturado
    /// </summary>
    public string? GetLastActivationCode()
    {
        lock (_lock)
        {
            return _capturedEmails
                .Where(e => e.Type == EmailType.AccountActivation && !string.IsNullOrEmpty(e.Token))
                .OrderByDescending(e => e.SentAt)
                .FirstOrDefault()?.Token;
        }
    }

    /// <summary>
    /// Obtém o último token de redefinição de senha capturado
    /// </summary>
    public string? GetLastPasswordResetToken()
    {
        lock (_lock)
        {
            return _capturedEmails
                .Where(e => e.Type == EmailType.PasswordReset && !string.IsNullOrEmpty(e.Token))
                .OrderByDescending(e => e.SentAt)
                .FirstOrDefault()?.Token;
        }
    }

    /// <summary>
    /// Obtém todos os emails capturados
    /// </summary>
    public IReadOnlyList<CapturedEmail> GetAllCapturedEmails()
    {
        lock (_lock)
        {
            return _capturedEmails.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Obtém emails capturados por tipo
    /// </summary>
    public IReadOnlyList<CapturedEmail> GetCapturedEmailsByType(EmailType type)
    {
        lock (_lock)
        {
            return _capturedEmails
                .Where(e => e.Type == type)
                .OrderByDescending(e => e.SentAt)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Obtém emails capturados para um endereço específico
    /// </summary>
    public IReadOnlyList<CapturedEmail> GetCapturedEmailsForAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new List<CapturedEmail>().AsReadOnly();

        lock (_lock)
        {
            return _capturedEmails
                .Where(e => string.Equals(e.Email, email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.SentAt)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Limpa todos os dados capturados
    /// </summary>
    public void ClearCapturedData()
    {
        lock (_lock)
        {
            _capturedEmails.Clear();
        }

        _logger.LogInformation("Dados de emails capturados foram limpos");
    }

    /// <summary>
    /// Obtém a contagem total de emails capturados
    /// </summary>
    public int GetCapturedEmailsCount()
    {
        lock (_lock)
        {
            return _capturedEmails.Count;
        }
    }
}

/// <summary>
/// Representa um email capturado pelo serviço fake
/// </summary>
public class CapturedEmail
{
    public EmailType Type { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Token { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Tipos de email suportados
/// </summary>
public enum EmailType
{
    AccountActivation,
    Welcome,
    PasswordReset
}