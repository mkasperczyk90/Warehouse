namespace Warehouse.Gateway.Bff;

/// <summary>
/// The terminal Task-hub BFF: fans out to Inventory and Logistics to count the operator's open work —
/// deliveries to receive, pallets to put away, pick tasks released to the floor, and replenishment
/// moves — all scoped to the operator's warehouse. Best-effort (a failing source counts zero), mirroring
/// <see cref="WorklistService"/>. The gateway is the only place that may fan out across services.
/// </summary>
public sealed class TerminalTasksService(BffFetch fetch)
{
    private const string DefaultWarehouse = "WH01";

    private static readonly HashSet<string> Receivable =
        new(StringComparer.OrdinalIgnoreCase) { "Announced", "Arrived", "Receiving" };

    public async Task<IReadOnlyList<TerminalTaskDto>> BuildAsync(string? warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = string.IsNullOrWhiteSpace(warehouseId) ? DefaultWarehouse : warehouseId;
        var warehousing = fetch.Client(BffClients.Warehousing, warehouse);
        var logistics = fetch.Client(BffClients.Logistics, warehouse);

        var deliveries = fetch.GetListAsync<DeliverySummaryView>(logistics, "logistics/deliveries", cancellationToken);
        var putAway = fetch.GetListAsync<PutAwayTaskView>(warehousing, $"inventory/put-away/tasks?warehouse={warehouse}", cancellationToken);
        var moves = fetch.GetListAsync<MoveTaskView>(warehousing, $"inventory/moves?warehouse={warehouse}", cancellationToken);
        var orders = fetch.GetListAsync<OrderSummaryView>(logistics, "logistics/orders", cancellationToken);

        await Task.WhenAll(deliveries, putAway, moves, orders);

        var receive = deliveries.Result.Count(d => Same(d.WarehouseCode, warehouse) && Receivable.Contains(d.Status));

        // Pick is counted in pending tasks across the orders released to the floor (status Picking).
        var pickingOrders = orders.Result.Where(o => Same(o.WarehouseCode, warehouse) && o.Status == "Picking").ToList();
        var pickLists = await Task.WhenAll(pickingOrders.Select(o =>
            fetch.GetAsync<PickListView>(logistics, $"logistics/orders/{o.Id}/pick-list", cancellationToken)));
        var pick = pickLists.Where(p => p is not null)
            .Sum(p => p!.Tasks.Count(t => string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase)));

        var putAwayCount = putAway.Result.Count;
        var moveCount = moves.Result.Count;

        return
        [
            new TerminalTaskDto("receive", Detail(receive, "delivery", "deliveries", "to receive"), receive),
            new TerminalTaskDto("putaway", Detail(putAwayCount, "pallet", "pallets", "in dock buffer"), putAwayCount),
            new TerminalTaskDto("pick", Detail(pick, "line", "lines", "to pick"), pick),
            new TerminalTaskDto("move", Detail(moveCount, "task", "tasks", "to replenish"), moveCount),
        ];
    }

    private static bool Same(string? a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string Detail(int n, string singular, string plural, string suffix) =>
        $"{n} {(n == 1 ? singular : plural)} {suffix}";
}
