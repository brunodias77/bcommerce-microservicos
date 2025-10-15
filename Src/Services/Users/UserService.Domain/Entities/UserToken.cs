using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

[Table("user_tokens")]
public class UserToken : Entity
{
    [Key]
    [Column("token_id")]
    public Guid TokenId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("token_type")]
    public UserTokenType TokenType { get; set; }

    [Required]
    [MaxLength(2048)]
    [Column("token_value")]
    public string TokenValue { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public override ValidationHandler Validate()
    {
        var validationHandler = new ValidationHandler();

        // Validação do token value
        if (string.IsNullOrWhiteSpace(TokenValue))
            validationHandler.AddError("TokenValue", "O valor do token é obrigatório");
        else if (TokenValue.Length > 2048)
            validationHandler.AddError("TokenValue", "O valor do token deve ter no máximo 2048 caracteres");

        // Validação da data de expiração
        if (ExpiresAt <= DateTime.UtcNow)
            validationHandler.AddError("ExpiresAt", "A data de expiração deve ser no futuro");

        // Validação do UserId
        if (UserId == Guid.Empty)
            validationHandler.AddError("UserId", "O ID do usuário é obrigatório");

        // Validação da data de revogação
        if (RevokedAt.HasValue && RevokedAt.Value > DateTime.UtcNow)
            validationHandler.AddError("RevokedAt", "A data de revogação não pode ser no futuro");

        return validationHandler;
    }
}