using System.Diagnostics.Metrics;
using Observability.Abstractions;

namespace Observability.OpenTelemetry;

public class MetricsService : IMetricsService
{
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _orderCreatedCounter;
    private readonly Counter<long> _paymentProcessedCounter;
    private readonly Counter<long> _cartOperationCounter;

    // Histograms
    private readonly Histogram<double> _apiLatencyHistogram;
    private readonly Histogram<double> _databaseQueryHistogram;
    private readonly Histogram<double> _cartOperationHistogram;

    public MetricsService(string serviceName)
    {
        _meter = new Meter(serviceName, "1.0.0");

        // Counters
        _orderCreatedCounter = _meter.CreateCounter<long>(
            "orders_created_total",
            description: "Total number of orders created");

        _paymentProcessedCounter = _meter.CreateCounter<long>(
            "payments_processed_total",
            description: "Total number of payments processed");

        _cartOperationCounter = _meter.CreateCounter<long>(
            "cart_operations_total",
            description: "Total number of cart operations");

        // Histograms
        _apiLatencyHistogram = _meter.CreateHistogram<double>(
            "api_latency_ms",
            unit: "ms",
            description: "API request latency in milliseconds");

        _databaseQueryHistogram = _meter.CreateHistogram<double>(
            "database_query_duration_ms",
            unit: "ms",
            description: "Database query duration in milliseconds");

        _cartOperationHistogram = _meter.CreateHistogram<double>(
            "cart_operation_duration_ms",
            unit: "ms",
            description: "Cart operation duration in milliseconds");
    }

    public void RecordOrderCreated(string status)
    {
        _orderCreatedCounter.Add(1, new KeyValuePair<string, object?>("status", status));
    }

    public void RecordPaymentProcessed(string paymentMethod, bool success)
    {
        _paymentProcessedCounter.Add(1,
            new KeyValuePair<string, object?>("payment_method", paymentMethod),
            new KeyValuePair<string, object?>("success", success.ToString()));
    }

    public void RecordCartOperation(string operation, double durationMs)
    {
        _cartOperationCounter.Add(1, new KeyValuePair<string, object?>("operation", operation));
        _cartOperationHistogram.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordApiLatency(string endpoint, double durationMs)
    {
        _apiLatencyHistogram.Record(durationMs, new KeyValuePair<string, object?>("endpoint", endpoint));
    }

    public void RecordDatabaseQuery(string operation, double durationMs)
    {
        _databaseQueryHistogram.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
    }
}
