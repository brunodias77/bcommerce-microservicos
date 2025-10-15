using System.Runtime.Serialization;

namespace UserService.Domain.Enums;

public enum UserRole
{
    [EnumMember(Value = "customer")]
    Customer,
    
    [EnumMember(Value = "admin")]
    Admin
}