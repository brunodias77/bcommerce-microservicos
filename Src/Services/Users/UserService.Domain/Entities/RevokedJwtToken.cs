using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;
using UserService.Domain.Aggregates;

namespace UserService.Domain.Entities;

[Table("revoked_jwt_tokens")]
public class RevokedJwtToken : Entity
{
    [Key]
    [Column("jti")]
    public Guid Jti { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public override ValidationHandler Validate()
    {
        var validation = new ValidationHandler();

        if (Jti == Guid.Empty)
            validation.Add("JTI_REQUIRED", "JTI é obrigatório");

        if (UserId == Guid.Empty)
            validation.Add("USER_ID_REQUIRED", "UserId é obrigatório");

        if (ExpiresAt <= DateTime.UtcNow)
            validation.Add("EXPIRES_AT_INVALID", "ExpiresAt deve ser uma data futura");

        return validation;
    }
}