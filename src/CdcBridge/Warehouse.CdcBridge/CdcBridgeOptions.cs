namespace Warehouse.CdcBridge;

/// <summary>
/// Bound from the <c>"CdcBridge"</c> configuration section. Mirrors the shape of
/// <c>Warehouse.ServiceDefaults.BiExport.BiExportOptions</c> (same bucket, sibling prefix) — see
/// docs/BI_Plan.md's CDC section for why CDC output lands under <c>bronze-cdc/</c> rather than the
/// event-driven writer's <c>bronze/</c>.
/// </summary>
public sealed class CdcBridgeOptions
{
    public const string SectionName = "CdcBridge";

    /// <summary>Same BI lake bucket the event-driven BiExport writer uses.</summary>
    public string BucketName { get; init; } = "";

    public string Region { get; init; } = "eu-central-1";

    public string RabbitMqHost { get; init; } = "localhost";

    public int RabbitMqPort { get; init; } = 5672;

    public string RabbitMqUsername { get; init; } = "guest";

    public string RabbitMqPassword { get; init; } = "guest";

    /// <summary>
    /// One topic exchange per source database that a Debezium Server instance publishes CDC records
    /// onto (see infra/terraform/environment/locals.tf's <c>cdc_exchange_*</c> locals). The bridge
    /// binds one durable queue per exchange with routing key <c>#</c> — table-level filtering already
    /// happened in Debezium's own <c>table.include.list</c>, so no finer routing is needed here.
    /// </summary>
    public IReadOnlyList<string> Exchanges { get; init; } = [];
}
