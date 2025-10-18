using BuildingBlocks.Results;

namespace UserService.Infrastructure.Contracts;

/// <summary>
/// Interface para serviços de envio de email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia email de ativação de conta para o usuário
    /// </summary>
    /// <param name="email">Email do destinatário</param>
    /// <param name="firstName">Nome do usuário</param>
    /// <param name="activationToken">Token de ativação único</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Result indicando sucesso ou falha</returns>
    Task<Result> SendAccountActivationEmailAsync(
        string email, 
        string firstName, 
        string activationToken, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email de boas-vindas após ativação da conta
    /// </summary>
    /// <param name="email">Email do destinatário</param>
    /// <param name="firstName">Nome do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Result indicando sucesso ou falha</returns>
    Task<Result> SendWelcomeEmailAsync(
        string email, 
        string firstName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email de redefinição de senha
    /// </summary>
    /// <param name="email">Email do destinatário</param>
    /// <param name="firstName">Nome do usuário</param>
    /// <param name="resetToken">Token de redefinição de senha</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Result indicando sucesso ou falha</returns>
    Task<Result> SendPasswordResetEmailAsync(
        string email, 
        string firstName, 
        string resetToken, 
        CancellationToken cancellationToken = default);
}