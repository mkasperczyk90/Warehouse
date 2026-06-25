namespace Warehouse.Gateway.Bff;

// --- Output: the worklist the admin Today screen renders (matches its `Worklist` model) -------------

public sealed record WorklistDto(WorklistCountsDto Counts, IReadOnlyList<WorklistQueueDto> Queues);

public sealed record WorklistCountsDto(int Qc, int Expiring, int Partial, int Inbound, int Stocktake);

public sealed record WorklistQueueDto(string Key, int Count, string? ShownNote, IReadOnlyList<WorklistItemDto> Items);

public sealed record WorklistItemDto(string Id, string Label, string Sublabel, BadgeDto? Badge, string? Meta);

public sealed record BadgeDto(string Variant, string Label);

// --- Input: the slices of each service's read model the worklist consumes (deserialized case-insensitively).
// The gateway keeps its own minimal shapes so it is not coupled to the services' full DTOs. ----------

internal sealed record QcBatchView(
    string Id, string Batch, string Product, string Location, string FromReceipt,
    decimal Qty, string Unit, string Status, string StatusLabel);

internal sealed record StockRowView(
    string Id, string Product, string Batch, string Location, string Room, string BestBefore, string Status);

internal sealed record OrderSummaryView(
    string Id, string WarehouseCode, DateTimeOffset RequiredAt, string Status, int LineCount);

internal sealed record DeliverySummaryView(
    string Id, string WarehouseCode, DateTimeOffset PlannedAt, string Status, int LineCount);

internal sealed record StocktakeItemView(string Id, string State);
