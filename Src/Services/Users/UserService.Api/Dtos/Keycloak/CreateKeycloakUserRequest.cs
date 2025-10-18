using System.ComponentModel.DataAnnotations;

namespace UserService.Api.DTOs.Keycloak;

/// <summary>
/// DTO para requisição de criação de usuário no Keycloak
/// </summary>
public record CreateKeycloakUserRequest(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string FirstName,

    [Required]
    [StringLength(155, MinimumLength = 2)]
    string LastName,

    [Required]
    [EmailAddress]
    [StringLength(255)]
    string Email,

    [Required]
    [StringLength(255, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "A senha deve ter no mínimo 8 caracteres e incluir pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial.")]
    string Password,

    bool Enabled = true,
    bool EmailVerified = false,
    List<string>? Roles = null
);