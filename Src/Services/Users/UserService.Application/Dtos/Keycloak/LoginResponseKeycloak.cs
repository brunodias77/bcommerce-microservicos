using System.Text.Json.Serialization;

namespace UserService.Application.Dtos.Keycloak;

public record LoginResponseKeycloak(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("scope")] string Scope
);