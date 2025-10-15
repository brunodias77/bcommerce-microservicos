using System.Runtime.Serialization;

namespace UserService.Domain.Enums;

public enum ConsentType
{
    [EnumMember(Value = "marketing_email")]
    MarketingEmail,
    
    [EnumMember(Value = "newsletter_subscription")]
    NewsletterSubscription,
    
    [EnumMember(Value = "terms_of_service")]
    TermsOfService,
    
    [EnumMember(Value = "privacy_policy")]
    PrivacyPolicy,
    
    [EnumMember(Value = "cookies_essential")]
    CookiesEssential,
    
    [EnumMember(Value = "cookies_analytics")]
    CookiesAnalytics,
    
    [EnumMember(Value = "cookies_marketing")]
    CookiesMarketing
}