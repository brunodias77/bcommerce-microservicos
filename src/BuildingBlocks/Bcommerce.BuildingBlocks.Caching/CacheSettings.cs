namespace Bcommerce.BuildingBlocks.Caching;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";
    public string ConnectionString { get; set; } = "localhost";
    public int DefaultExpirationInMinutes { get; set; } = 60;
}
