using System.Runtime.Serialization;

namespace UserService.Domain.Enums;

public enum UserStatus
{
    [EnumMember(Value = "ativo")]
    Ativo,
    
    [EnumMember(Value = "inativo")]
    Inativo,
    
    [EnumMember(Value = "banido")]
    Banido
}