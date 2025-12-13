namespace Common.Infrastructure.Outbox;

/// <summary>
/// Mensagem do Outbox Pattern para garantir entrega de eventos
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; } = default!;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public bool IsProcessed => ProcessedAt.HasValue;
    public bool CanRetry => RetryCount < 5; // Máximo de 5 tentativas
}