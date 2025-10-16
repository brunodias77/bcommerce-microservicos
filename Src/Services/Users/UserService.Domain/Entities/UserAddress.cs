using BuildingBlocks.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Validations;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

[Table("user_addresses")]
public class UserAddress : Entity
{
    [Key]
    [Column("address_id")]
    public Guid AddressId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("type")]
    public AddressType Type { get; set; }

    [Required]
    [StringLength(8, MinimumLength = 8)]
    [Column("postal_code")]
    public string PostalCode { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("street")]
    public string Street { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("street_number")]
    public string StreetNumber { get; set; }

    [MaxLength(100)]
    [Column("complement")]
    public string? Complement { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("neighborhood")]
    public string Neighborhood { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("city")]
    public string City { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    [Column("state_code")]
    public string StateCode { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    [Column("country_code")]
    public string CountryCode { get; set; } = "BR";

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
            validationHandler.Add("UserId", "O ID do usuário é obrigatório");

        // Validação do CEP
        if (string.IsNullOrWhiteSpace(PostalCode))
            validationHandler.Add("PostalCode", "O CEP é obrigatório");
        else if (PostalCode.Length != 8)
            validationHandler.Add("PostalCode", "O CEP deve ter exatamente 8 caracteres");

        // Validação da rua
        if (string.IsNullOrWhiteSpace(Street))
            validationHandler.Add("Street", "A rua é obrigatória");
        else if (Street.Length > 150)
            validationHandler.Add("Street", "A rua deve ter no máximo 150 caracteres");

        // Validação do número
        if (string.IsNullOrWhiteSpace(StreetNumber))
            validationHandler.Add("StreetNumber", "O número é obrigatório");
        else if (StreetNumber.Length > 20)
            validationHandler.Add("StreetNumber", "O número deve ter no máximo 20 caracteres");

        // Validação do bairro
        if (string.IsNullOrWhiteSpace(Neighborhood))
            validationHandler.Add("Neighborhood", "O bairro é obrigatório");
        else if (Neighborhood.Length > 100)
            validationHandler.Add("Neighborhood", "O bairro deve ter no máximo 100 caracteres");

        // Validação da cidade
        if (string.IsNullOrWhiteSpace(City))
            validationHandler.Add("City", "A cidade é obrigatória");
        else if (City.Length > 100)
            validationHandler.Add("City", "A cidade deve ter no máximo 100 caracteres");

        // Validação do código do estado
        if (string.IsNullOrWhiteSpace(StateCode))
            validationHandler.Add("StateCode", "O código do estado é obrigatório");
        else if (StateCode.Length != 2)
            validationHandler.Add("StateCode", "O código do estado deve ter exatamente 2 caracteres");

        // Validação do código do país
        if (string.IsNullOrWhiteSpace(CountryCode))
            validationHandler.Add("CountryCode", "O código do país é obrigatório");
        else if (CountryCode.Length != 2)
            validationHandler.Add("CountryCode", "O código do país deve ter exatamente 2 caracteres");

        // Validação do complemento
        if (!string.IsNullOrWhiteSpace(Complement) && Complement.Length > 100)
            validationHandler.Add("Complement", "O complemento deve ter no máximo 100 caracteres");

        return validationHandler;
    }
}