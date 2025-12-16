using System.Diagnostics;

namespace Bcommerce.BuildingBlocks.Observability.Tracing;

public static class ActivityExtensions
{
    public static void AdicionarTag(this Activity activity, string chave, object valor)
    {
        activity?.SetTag(chave, valor);
    }

    public static void AdicionarEvento(this Activity activity, string nome)
    {
        activity?.AddEvent(new ActivityEvent(nome));
    }
}
