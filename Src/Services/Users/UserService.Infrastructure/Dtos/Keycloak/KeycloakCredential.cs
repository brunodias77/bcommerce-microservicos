using System.Text.Json.Serialization;

namespace UserService.Infrastructure.Dtos.Keycloak;

public record KeycloakCredential(
    [property: JsonPropertyName("type")] string Type = "password",
    [property: JsonPropertyName("value")] string Value = "",
    [property: JsonPropertyName("temporary")] bool Temporary = false
);