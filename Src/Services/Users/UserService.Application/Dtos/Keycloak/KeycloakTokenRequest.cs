using System.Text.Json.Serialization;

namespace UserService.Application.Dtos.Keycloak;

public record KeycloakTokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType = "password",
    [property: JsonPropertyName("client_id")] string ClientId = "",
    [property: JsonPropertyName("client_secret")] string? ClientSecret = null,
    [property: JsonPropertyName("username")] string Username = "",
    [property: JsonPropertyName("password")] string Password = "",
    [property: JsonPropertyName("scope")] string Scope = "openid"
);