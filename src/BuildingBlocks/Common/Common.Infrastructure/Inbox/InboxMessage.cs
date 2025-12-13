namespace Common.Infrastructure.Inbox;

/// <summary>
/// Mensagem do Inbox Pattern para garantir idempotência
/// </summary>
public class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = default!;
    public DateTime ProcessedAt { get; set; }

    public InboxMessage()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public InboxMessage(Guid id, string messageType)
    {
        Id = id;
        MessageType = messageType;
        ProcessedAt = DateTime.UtcNow;
    }
}