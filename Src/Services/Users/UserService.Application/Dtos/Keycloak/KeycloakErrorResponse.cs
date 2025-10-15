using System.Text.Json.Serialization;

namespace UserService.Application.Dtos.Keycloak;

public record KeycloakErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription
);