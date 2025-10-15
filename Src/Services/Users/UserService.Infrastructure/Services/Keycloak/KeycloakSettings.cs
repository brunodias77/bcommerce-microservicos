namespace UserService.Infrastructure.Services.Keycloak;

public class KeycloakSettings
{
    public const string SectionName = "Keycloak";

    public string Url { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string BackendClientId { get; set; } = string.Empty;
    public string BackendClientSecret { get; set; } = string.Empty;
    public string FrontendClientId { get; set; } = string.Empty;
    public int TokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}