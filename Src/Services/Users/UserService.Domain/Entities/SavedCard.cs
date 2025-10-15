using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

[Table("user_saved_cards")]
public class SavedCard : Entity
{
    [Key]
    [Column("saved_card_id")]
    public Guid SavedCardId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [MaxLength(50)]
    [Column("nickname")]
    public string? Nickname { get; set; }

    [Required]
    [StringLength(4, MinimumLength = 4)]
    [Column("last_four_digits")]
    public string LastFourDigits { get; set; }

    [Required]
    [Column("brand")]
    public CardBrand Brand { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("gateway_token")]
    public string GatewayToken { get; set; }

    [Required]
    [Column("expiry_date")]
    [DataType(DataType.Date)]
    public DateTime ExpiryDate { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public override ValidationHandler Validate()
    {
        var validationHandler = new ValidationHandler();

        // Validação do UserId
        if (UserId == Guid.Empty)
            validationHandler.AddError("UserId", "O ID do usuário é obrigatório");

        // Validação dos últimos 4 dígitos
        if (string.IsNullOrWhiteSpace(LastFourDigits))
            validationHandler.AddError("LastFourDigits", "Os últimos 4 dígitos são obrigatórios");
        else if (LastFourDigits.Length != 4)
            validationHandler.AddError("LastFourDigits", "Devem ser exatamente 4 dígitos");
        else if (!LastFourDigits.All(char.IsDigit))
            validationHandler.AddError("LastFourDigits", "Devem conter apenas números");

        // Validação do token do gateway
        if (string.IsNullOrWhiteSpace(GatewayToken))
            validationHandler.AddError("GatewayToken", "O token do gateway é obrigatório");
        else if (GatewayToken.Length > 255)
            validationHandler.AddError("GatewayToken", "O token do gateway deve ter no máximo 255 caracteres");

        // Validação da data de expiração
        if (ExpiryDate <= DateTime.Today)
            validationHandler.AddError("ExpiryDate", "A data de expiração deve ser no futuro");

        // Validação do apelido
        if (!string.IsNullOrWhiteSpace(Nickname) && Nickname.Length > 50)
            validationHandler.AddError("Nickname", "O apelido deve ter no máximo 50 caracteres");

        return validationHandler;
    }
}