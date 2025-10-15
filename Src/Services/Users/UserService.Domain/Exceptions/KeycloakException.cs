namespace UserService.Domain.Exceptions;

/// <summary>
/// Exception customizada para erros relacionados a operações do Keycloak
/// </summary>
public class KeycloakException : Exception
{
    /// <summary>
    /// Código do erro específico
    /// </summary>
    public string? ErrorCode { get; }
    
    /// <summary>
    /// Operação que estava sendo executada quando o erro ocorreu
    /// </summary>
    public string? Operation { get; }
    
    /// <summary>
    /// ID do usuário relacionado ao erro (se aplicável)
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Construtor padrão
    /// </summary>
    /// <param name="message">Mensagem de erro</param>
    public KeycloakException(string message) : base(message)
    {
    }

    /// <summary>
    /// Construtor com exceção interna
    /// </summary>
    /// <param name="message">Mensagem de erro</param>
    /// <param name="innerException">Exceção interna</param>
    public KeycloakException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Construtor completo
    /// </summary>
    /// <param name="message">Mensagem de erro</param>
    /// <param name="errorCode">Código do erro</param>
    /// <param name="operation">Operação sendo executada</param>
    /// <param name="userId">ID do usuário (se aplicável)</param>
    /// <param name="innerException">Exceção interna</param>
    public KeycloakException(string message, string? errorCode = null, string? operation = null, string? userId = null, Exception? innerException = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Operation = operation;
        UserId = userId;
    }

    /// <summary>
    /// Cria uma KeycloakException para erros de criação de usuário
    /// </summary>
    /// <param name="email">Email do usuário</param>
    /// <param name="details">Detalhes do erro</param>
    /// <param name="innerException">Exceção interna</param>
    /// <returns>Nova instância de KeycloakException</returns>
    public static KeycloakException ForUserCreationError(string email, string details, Exception? innerException = null)
    {
        return new KeycloakException(
            $"Erro ao criar usuário no Keycloak para email '{email}': {details}",
            "USER_CREATION_ERROR",
            "CREATE_USER",
            email,
            innerException
        );
    }

    /// <summary>
    /// Cria uma KeycloakException para erros de exclusão de usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="details">Detalhes do erro</param>
    /// <param name="innerException">Exceção interna</param>
    /// <returns>Nova instância de KeycloakException</returns>
    public static KeycloakException ForUserDeletionError(string userId, string details, Exception? innerException = null)
    {
        return new KeycloakException(
            $"Erro ao excluir usuário no Keycloak com ID '{userId}': {details}",
            "USER_DELETION_ERROR",
            "DELETE_USER",
            userId,
            innerException
        );
    }

    /// <summary>
    /// Cria uma KeycloakException para erros de autenticação
    /// </summary>
    /// <param name="details">Detalhes do erro</param>
    /// <param name="innerException">Exceção interna</param>
    /// <returns>Nova instância de KeycloakException</returns>
    public static KeycloakException ForAuthenticationError(string details, Exception? innerException = null)
    {
        return new KeycloakException(
            $"Erro de autenticação com Keycloak: {details}",
            "AUTHENTICATION_ERROR",
            "AUTHENTICATE",
            innerException: innerException
        );
    }

    /// <summary>
    /// Cria uma KeycloakException para erros de conexão
    /// </summary>
    /// <param name="details">Detalhes do erro de conexão</param>
    /// <param name="innerException">Exceção interna</param>
    /// <returns>Nova instância de KeycloakException</returns>
    public static KeycloakException ForConnectionError(string details, Exception? innerException = null)
    {
        return new KeycloakException(
            $"Erro de conexão com Keycloak: {details}",
            "CONNECTION_ERROR",
            "CONNECTION",
            innerException: innerException
        );
    }

    /// <summary>
    /// Retorna uma representação em string da exceção com informações detalhadas
    /// </summary>
    /// <returns>String formatada com detalhes da exceção</returns>
    public override string ToString()
    {
        var details = base.ToString();
        
        if (!string.IsNullOrEmpty(ErrorCode))
            details += $"\nCódigo do Erro: {ErrorCode}";
            
        if (!string.IsNullOrEmpty(Operation))
            details += $"\nOperação: {Operation}";
            
        if (!string.IsNullOrEmpty(UserId))
            details += $"\nUsuário ID: {UserId}";
            
        return details;
    }
}