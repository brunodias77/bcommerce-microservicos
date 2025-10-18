using System.Text.Json.Serialization;

namespace UserService.Infrastructure.Dtos.Keycloak;


public record KeycloakRoleRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description
);