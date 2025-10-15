using System.Runtime.Serialization;

namespace UserService.Domain.Enums;

public enum AddressType
{
    [EnumMember(Value = "shipping")]
    Shipping,
    
    [EnumMember(Value = "billing")]
    Billing
}
