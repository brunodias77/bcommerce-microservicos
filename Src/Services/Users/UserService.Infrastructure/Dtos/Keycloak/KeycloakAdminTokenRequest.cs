using System.Text.Json.Serialization;

namespace UserService.Infrastructure.Dtos.Keycloak;

public record KeycloakAdminTokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType = "password",
    [property: JsonPropertyName("client_id")] string ClientId = "admin-cli",
    [property: JsonPropertyName("username")] string Username = "",
    [property: JsonPropertyName("password")] string Password = ""
);