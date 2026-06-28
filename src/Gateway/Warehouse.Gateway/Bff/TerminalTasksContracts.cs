namespace Warehouse.Gateway.Bff;

// --- Output: the terminal Task hub's work piles (matches its `TaskData` model) ----------------------

/// <summary>One work pile on the handheld's Task hub: its kind, a short data detail, and a count.</summary>
public sealed record TerminalTaskDto(string Kind, string Detail, int Count);

// --- Input: minimal slices of each service's read model the hub aggregator counts -------------------
// (DeliverySummaryView / OrderSummaryView are reused from WorklistContracts.) ------------------------

internal sealed record PutAwayTaskView(string Sku);

internal sealed record MoveTaskView(string Sku);

internal sealed record PickListView(IReadOnlyList<PickTaskView> Tasks);

internal sealed record PickTaskView(string Status);
