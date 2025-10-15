using BuildingBlocks.Domain;
using BuildingBlocks.Validations;
using UserService.Domain.Enums;

namespace UserService.Domain.ValueObjects;

public class CardInfo : ValueObject
{
    public string LastFourDigits { get; private set; }
    public CardBrand Brand { get; private set; }
    public DateTime ExpiryDate { get; private set; }

    private CardInfo(string lastFourDigits, CardBrand brand, DateTime expiryDate)
    {
        LastFourDigits = lastFourDigits;
        Brand = brand;
        ExpiryDate = expiryDate;
    }

    public static CardInfo Create(string lastFourDigits, CardBrand brand, DateTime expiryDate)
    {
        if (string.IsNullOrWhiteSpace(lastFourDigits))
            throw new ArgumentException("Últimos 4 dígitos são obrigatórios", nameof(lastFourDigits));

        if (lastFourDigits.Length != 4 || !lastFourDigits.All(char.IsDigit))
            throw new ArgumentException("Últimos 4 dígitos devem conter exatamente 4 números", nameof(lastFourDigits));

        if (expiryDate <= DateTime.Now)
            throw new ArgumentException("Data de expiração deve ser futura", nameof(expiryDate));

        return new CardInfo(lastFourDigits, brand, expiryDate);
    }

    public bool IsExpired() => ExpiryDate <= DateTime.Now;

    public override ValidationHandler Validate()
    {
        var validation = new ValidationHandler();
        
        if (string.IsNullOrWhiteSpace(LastFourDigits))
        {
            validation.Add("CARD_LAST_FOUR_REQUIRED", "Últimos 4 dígitos são obrigatórios");
        }
        else if (LastFourDigits.Length != 4 || !LastFourDigits.All(char.IsDigit))
        {
            validation.Add("CARD_LAST_FOUR_INVALID", "Últimos 4 dígitos devem conter exatamente 4 números");
        }

        if (ExpiryDate <= DateTime.Now)
        {
            validation.Add("CARD_EXPIRED", "Cartão expirado");
        }

        return validation;
    }

    public string GetMaskedNumber()
    {
        return $"**** **** **** {LastFourDigits}";
    }

    public override string ToString() => GetMaskedNumber();
}