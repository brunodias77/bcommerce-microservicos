using System.Runtime.Serialization;

namespace UserService.Domain.Enums;

public enum UserTokenType
{
    [EnumMember(Value = "refresh")]
    Refresh,
    
    [EnumMember(Value = "email_verification")]
    EmailVerification,
    
    [EnumMember(Value = "password_reset")]
    PasswordReset
}