using System.Text.Json.Serialization;

namespace UserService.Infrastructure.Dtos.Keycloak;

public record KeycloakUserRepresentation(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("enabled")] bool Enabled = true,
    [property: JsonPropertyName("emailVerified")] bool EmailVerified = false,
    [property: JsonPropertyName("createdTimestamp")] long? CreatedTimestamp = null,
    [property: JsonPropertyName("credentials")] List<KeycloakCredential>? Credentials = null,
    [property: JsonPropertyName("attributes")] Dictionary<string, List<string>>? Attributes = null
);