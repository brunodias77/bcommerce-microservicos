using System.Runtime.Serialization;

namespace UserService.Domain.Enums;

public enum CardBrand
{
    [EnumMember(Value = "visa")]
    Visa,
    
    [EnumMember(Value = "mastercard")]
    Mastercard,
    
    [EnumMember(Value = "amex")]
    Amex,
    
    [EnumMember(Value = "elo")]
    Elo,
    
    [EnumMember(Value = "hipercard")]
    Hipercard,
    
    [EnumMember(Value = "diners_club")]
    DinersClub,
    
    [EnumMember(Value = "discover")]
    Discover,
    
    [EnumMember(Value = "jcb")]
    Jcb,
    
    [EnumMember(Value = "aura")]
    Aura,
    
    [EnumMember(Value = "other")]
    Other
}