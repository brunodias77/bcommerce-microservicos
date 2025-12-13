namespace Observability.OpenTelemetry;

public class OpenTelemetrySettings
{
    public bool UseConsoleExporter { get; set; } = false;
    public bool UseJaeger { get; set; } = true;
    public string JaegerEndpoint { get; set; } = "localhost";
    public int JaegerPort { get; set; } = 6831;
    public bool UseOtlpExporter { get; set; } = false;
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public bool UsePrometheus { get; set; } = true;
}