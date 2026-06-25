using Warehouse.Gateway.Bff;
using Xunit;

namespace Warehouse.Gateway.Tests;

public sealed class SearchMapperTests
{
    private static readonly DateTimeOffset When = new(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Empty_query_returns_no_hits()
    {
        var hits = SearchMapper.Build("  ", [new ProductView("MILK-1L", "Milk")], [], [], [], []);
        Assert.Empty(hits);
    }

    [Fact]
    public void Matches_across_types_case_insensitively()
    {
        var hits = SearchMapper.Build(
            "milk",
            [new ProductView("MILK-1L", "Whole milk 1 L"), new ProductView("BOX-L", "Cardboard box")],
            [new SearchStockView("S1", "Whole milk 1 L", "MILK-1L", "LOT-1", "CR1-A01")],
            [new DeliverySummaryView("ASN-1", "WH01", When, "Announced", 2)],
            [new OrderSummaryView("SO-1", "WH01", When, "Created", 1)],
            [new LocationView("MILK-BAY", "Cold room CR1", "WH01")]);

        Assert.Equal(
            [("product", "MILK-1L"), ("stock", "S1"), ("location", "MILK-BAY")],
            hits.Select(h => (h.Type, h.RefId)));
        Assert.Equal("CR1-A01 · MILK-1L", hits.Single(h => h.Type == "stock").Sublabel);
    }

    [Fact]
    public void Matches_an_order_or_asn_by_id()
    {
        var hits = SearchMapper.Build(
            "SO-1", [], [],
            [new DeliverySummaryView("ASN-9", "WH01", When, "Announced", 1)],
            [new OrderSummaryView("SO-1", "WH01", When, "Created", 1)],
            []);

        var hit = Assert.Single(hits);
        Assert.Equal("order", hit.Type);
        Assert.Equal("SO-1", hit.RefId);
    }

    [Fact]
    public void Caps_results_at_eight()
    {
        var products = Enumerable.Range(1, 20)
            .Select(i => new ProductView($"SKU-{i}", $"Widget {i}")).ToList();

        var hits = SearchMapper.Build("widget", products, [], [], [], []);

        Assert.Equal(8, hits.Count);
    }
}
