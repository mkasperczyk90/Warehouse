using Warehouse.Gateway.Bff;
using Xunit;

namespace Warehouse.Gateway.Tests;

public sealed class WorklistMapperTests
{
    private static readonly DateOnly Today = new(2026, 6, 25);

    private static WorklistDto Build(
        IReadOnlyList<QcBatchView>? qc = null,
        IReadOnlyList<StockRowView>? stock = null,
        IReadOnlyList<OrderSummaryView>? orders = null,
        IReadOnlyList<DeliverySummaryView>? deliveries = null,
        IReadOnlyList<StocktakeItemView>? stocktakes = null) =>
        WorklistMapper.Build(
            qc ?? [], stock ?? [], orders ?? [], deliveries ?? [], stocktakes ?? [], Today);

    [Fact]
    public void Counts_aggregate_each_source()
    {
        var result = Build(
            qc: [Qc("B1"), Qc("B2")],
            orders: [Order("SO-1", "PartiallyReserved"), Order("SO-2", "Reserved")],
            deliveries: [Delivery("ASN-1", "Announced")],
            stocktakes: [new StocktakeItemView("ST-1", "review"), new StocktakeItemView("ST-2", "counting")]);

        Assert.Equal(2, result.Counts.Qc);
        Assert.Equal(1, result.Counts.Partial);   // only the PartiallyReserved order
        Assert.Equal(1, result.Counts.Inbound);
        Assert.Equal(1, result.Counts.Stocktake);  // only the one in review
    }

    [Fact]
    public void Expiring_keeps_only_stock_within_the_seven_day_window()
    {
        var result = Build(stock:
        [
            Stock("near", "2026-06-26"),   // +1 day → in
            Stock("edge", "2026-07-02"),   // +7 days → in
            Stock("far", "2026-07-10"),    // +15 days → out
            Stock("past", "2026-06-20"),   // already expired → out
            Stock("bad", "not-a-date"),    // unparseable → out
        ]);

        var expiring = result.Queues.Single(q => q.Key == "expiring");
        Assert.Equal(2, expiring.Count);
        Assert.Equal(["near", "edge"], expiring.Items.Select(i => i.Id));
        Assert.Equal("BBE 1d", expiring.Items[0].Badge!.Label);
        Assert.Equal("expired", expiring.Items[0].Badge!.Variant);
    }

    [Fact]
    public void Partial_queue_filters_and_maps_status_to_a_badge_variant()
    {
        var result = Build(orders:
        [
            Order("SO-1", "PartiallyReserved"),
            Order("SO-2", "Created"),
            Order("SO-3", "Dispatched"),  // excluded
        ]);

        var partial = result.Queues.Single(q => q.Key == "partial");
        Assert.Equal(["SO-1", "SO-2"], partial.Items.Select(i => i.Id));
        Assert.Equal("transit", partial.Items[0].Badge!.Variant);
        Assert.Equal("Partially reserved", partial.Items[0].Badge!.Label); // humanized
    }

    [Fact]
    public void Queues_truncate_with_a_shown_note()
    {
        var qc = Enumerable.Range(1, 5).Select(i => Qc($"B{i}")).ToList();

        var queue = Build(qc: qc).Queues.Single(q => q.Key == "qc");

        Assert.Equal(5, queue.Count);
        Assert.Equal(3, queue.Items.Count);          // capped
        Assert.Equal("top 3 of 5", queue.ShownNote);
    }

    private static QcBatchView Qc(string id) =>
        new(id, $"LOT-{id}", "Cheese", "QC-HOLD", "GR-1", 10, "ea", "blocked", "On hold");

    private static StockRowView Stock(string id, string bestBefore) =>
        new(id, "Milk", "LOT-1", "CR1-A01", "Cold room", bestBefore, "available");

    private static OrderSummaryView Order(string id, string status) =>
        new(id, "WH01", new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero), status, 3);

    private static DeliverySummaryView Delivery(string id, string status) =>
        new(id, "WH01", new DateTimeOffset(2026, 6, 26, 0, 0, 0, TimeSpan.Zero), status, 2);
}
