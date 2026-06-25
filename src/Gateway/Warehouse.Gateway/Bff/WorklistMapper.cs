using System.Globalization;

namespace Warehouse.Gateway.Bff;

/// <summary>
/// Pure projection of the read models the BFF fetched into the admin's work-queue landing — "what needs
/// me now" (admin-10). No I/O, so the aggregation rules (expiring window, partial-order filter, the
/// status → badge-variant mapping the FE expects) are unit-testable without the services running.
/// </summary>
internal static class WorklistMapper
{
    /// <summary>How many days ahead still counts as "expiring soon".</summary>
    private const int ExpiringWindowDays = 7;

    private static readonly string[] PartialOrderStatuses = ["PartiallyReserved", "Created", "Picking"];

    public static WorklistDto Build(
        IReadOnlyList<QcBatchView> qc,
        IReadOnlyList<StockRowView> stock,
        IReadOnlyList<OrderSummaryView> orders,
        IReadOnlyList<DeliverySummaryView> deliveries,
        IReadOnlyList<StocktakeItemView> stocktakes,
        DateOnly today)
    {
        var expiring = ExpiringItems(stock, today);
        var partial = orders.Where(o => PartialOrderStatuses.Contains(o.Status)).ToList();
        var reviewStocktakes = stocktakes.Count(s => string.Equals(s.State, "review", StringComparison.OrdinalIgnoreCase));

        var qcQueue = new WorklistQueueDto("qc", qc.Count, ShownNote(qc.Count, 3),
            qc.Take(3).Select(b => new WorklistItemDto(
                b.Id,
                $"{b.Batch} · {b.Product}",
                $"{b.Location} · {b.FromReceipt}",
                new BadgeDto(b.Status, b.StatusLabel),
                $"{Trim(b.Qty)} {b.Unit}")).ToList());

        var partialQueue = new WorklistQueueDto("partial", partial.Count, ShownNote(partial.Count, 3),
            partial.Take(3).Select(o => new WorklistItemDto(
                o.Id,
                $"{o.Id} · {o.WarehouseCode}",
                $"Required {o.RequiredAt:yyyy-MM-dd} · {o.LineCount} lines",
                new BadgeDto(OrderVariant(o.Status), Humanize(o.Status)),
                null)).ToList());

        var expiringQueue = new WorklistQueueDto("expiring", expiring.Count, null, expiring);

        var inboundQueue = new WorklistQueueDto("inbound", deliveries.Count, ShownNote(deliveries.Count, 2),
            deliveries.Take(2).Select(d => new WorklistItemDto(
                d.Id,
                $"{d.Id} · {d.WarehouseCode}",
                $"{d.PlannedAt:yyyy-MM-dd} · {d.LineCount} lines",
                new BadgeDto(DeliveryVariant(d.Status), Humanize(d.Status)),
                null)).ToList());

        return new WorklistDto(
            new WorklistCountsDto(qc.Count, expiring.Count, partial.Count, deliveries.Count, reviewStocktakes),
            [qcQueue, partialQueue, expiringQueue, inboundQueue]);
    }

    private static List<WorklistItemDto> ExpiringItems(IReadOnlyList<StockRowView> stock, DateOnly today)
    {
        var items = new List<WorklistItemDto>();
        foreach (var r in stock)
        {
            if (!DateOnly.TryParse(r.BestBefore, CultureInfo.InvariantCulture, out var bbe))
            {
                continue;
            }

            var days = bbe.DayNumber - today.DayNumber;
            if (days < 0 || days > ExpiringWindowDays)
            {
                continue;
            }

            items.Add(new WorklistItemDto(
                r.Id,
                $"{r.Product} · {r.Batch}",
                $"{r.Location} · {r.Room}",
                new BadgeDto("expired", $"BBE {days}d"),
                r.BestBefore));
        }

        return items.OrderBy(_ => 0).ToList(); // keep input order (already best-before-ascending from the source)
    }

    /// <summary>"top N of M" when the queue is truncated; null when everything shown.</summary>
    private static string? ShownNote(int total, int shown) => total > shown ? $"top {shown} of {total}" : null;

    private static string OrderVariant(string status) => status switch
    {
        "PartiallyReserved" => "transit",
        "Picking" or "Packed" or "Dispatched" => "available",
        "Cancelled" => "blocked",
        _ => "reserved",
    };

    private static string DeliveryVariant(string status) => status switch
    {
        "Arrived" or "Receiving" or "Received" or "PutAwayInProgress" => "transit",
        "Completed" => "available",
        "Cancelled" => "blocked",
        _ => "reserved",
    };

    /// <summary>"PartiallyReserved" → "Partially reserved" — a readable badge label from the enum name.</summary>
    private static string Humanize(string pascal)
    {
        if (string.IsNullOrEmpty(pascal))
        {
            return pascal;
        }

        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        sb.Append(pascal[0]);
        for (var i = 1; i < pascal.Length; i++)
        {
            if (char.IsUpper(pascal[i]))
            {
                sb.Append(' ').Append(char.ToLowerInvariant(pascal[i]));
            }
            else
            {
                sb.Append(pascal[i]);
            }
        }

        return sb.ToString();
    }

    private static string Trim(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
