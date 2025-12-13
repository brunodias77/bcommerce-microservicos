namespace PaymentStripe.API.Configuration;

/// <summary>
/// Configurações do Stripe
/// </summary>
public class StripeSettings
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Chave secreta da API (sk_test_... ou sk_live_...)
    /// </summary>
    public string SecretKey { get; set; } = default!;

    /// <summary>
    /// Chave pública (pk_test_... ou pk_live_...)
    /// </summary>
    public string PublishableKey { get; set; } = default!;

    /// <summary>
    /// Segredo do webhook para validar assinaturas
    /// </summary>
    public string WebhookSecret { get; set; } = default!;

    /// <summary>
    /// Moeda padrão
    /// </summary>
    public string DefaultCurrency { get; set; } = "brl";

    /// <summary>
    /// URL de sucesso padrão para checkout
    /// </summary>
    public string? SuccessUrl { get; set; }

    /// <summary>
    /// URL de cancelamento padrão para checkout
    /// </summary>
    public string? CancelUrl { get; set; }
}
