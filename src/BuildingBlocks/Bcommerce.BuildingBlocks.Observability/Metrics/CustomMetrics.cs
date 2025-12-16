using System.Diagnostics.Metrics;

namespace Bcommerce.BuildingBlocks.Observability.Metrics;

public static class CustomMetrics
{
    public const string MeterName = "Bcommerce.Metrics";
    private static readonly Meter Meter = new(MeterName);

    public static Counter<int> CriarContador(string nome, string descricao)
    {
        return Meter.CreateCounter<int>(nome, description: descricao);
    }
    
    public static Histogram<double> CriarHistograma(string nome, string descricao)
    {
        return Meter.CreateHistogram<double>(nome, description: descricao);
    }
}
