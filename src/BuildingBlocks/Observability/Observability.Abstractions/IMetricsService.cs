namespace Observability.Abstractions;

public interface IMetricsService
{
    void RecordOrderCreated(string status);
    void RecordPaymentProcessed(string paymentMethod, bool success);
    void RecordCartOperation(string operation, double durationMs);
    void RecordApiLatency(string endpoint, double durationMs);
    void RecordDatabaseQuery(string operation, double durationMs);
}