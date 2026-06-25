namespace Warehouse.Gateway.Bff;

/// <summary>Named-HttpClient keys for the BFF fan-out. Each resolves to a logical service address
/// (Aspire service discovery) and inherits the standard resilience handler from ServiceDefaults.</summary>
internal static class BffClients
{
    public const string Warehousing = "warehousing";
    public const string Logistics = "logistics";
    public const string MasterData = "masterdata";
}
