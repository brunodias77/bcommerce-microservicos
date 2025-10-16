using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

[Table("user_consents")]
public class UserConsent : Entity
{
    [Key]
    [Column("consent_id")]
    public Guid ConsentId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("type")]
    public ConsentType Type { get; set; }

    [MaxLength(30)]
    [Column("terms_version")]
    public string? TermsVersion { get; set; }

    [Required]
    [Column("is_granted")]
    public bool IsGranted { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public override ValidationHandler Validate()
    {
        var validationHandler = new ValidationHandler();

        // Validação do UserId
        if (UserId == Guid.Empty)
            validationHandler.Add("UserId", "O ID do usuário é obrigatório");

        // Validação da versão dos termos
        if (!string.IsNullOrWhiteSpace(TermsVersion) && TermsVersion.Length > 30)
            validationHandler.Add("TermsVersion", "A versão dos termos deve ter no máximo 30 caracteres");

        return validationHandler;
    }
}