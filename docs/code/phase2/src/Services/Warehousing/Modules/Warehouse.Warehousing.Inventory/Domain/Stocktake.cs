using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain.Events;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): a blind count of selected locations (UC-07).
/// Counters never see expected quantities; on approval the differences become
/// adjustments in the ledger with reason StocktakeDifference.
/// </summary>
public sealed class Stocktake : AggregateRoot<StocktakeId>
{
    private readonly List<CountLine> _lines = [];

    private Stocktake(StocktakeId id, IReadOnlyCollection<LocationCode> scope, string orderedBy)
        : base(id)
    {
        Scope = scope;
        OrderedBy = orderedBy;
        Status = StocktakeStatus.Open;
        StartedAt = DateTimeOffset.UtcNow;
    }

    private Stocktake()
    {
    }

    public IReadOnlyCollection<LocationCode> Scope { get; private set; } = [];

    public string OrderedBy { get; private set; } = null!;

    public StocktakeStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public IReadOnlyCollection<CountLine> Lines => _lines.AsReadOnly();

    public static Stocktake Order(IReadOnlyCollection<LocationCode> scope, string orderedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderedBy);
        return scope is null || scope.Count == 0
            ? throw new DomainException("stocktake_scope_empty", "A stocktake needs at least one location to count.")
            : new Stocktake(StocktakeId.New(), scope, orderedBy);
    }

    public void RecordCount(LocationCode location, Sku sku, BatchNumber? batch, Quantity counted, Quantity expected, string countedBy)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(counted);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentException.ThrowIfNullOrWhiteSpace(countedBy);

        if (Status is not (StocktakeStatus.Open or StocktakeStatus.Counting))
        {
            throw new DomainException("stocktake_not_open", $"Stocktake {Id} is {Status} — counts can no longer be recorded.");
        }

        if (!Scope.Contains(location))
        {
            throw new DomainException("stocktake_location_out_of_scope", $"Location {location} is not in the scope of stocktake {Id}.");
        }

        Status = StocktakeStatus.Counting;
        _lines.Add(new CountLine(location, sku, batch, counted, expected, countedBy));
    }

    /// <summary>Closes the stocktake; the application layer turns difference lines into adjustments.</summary>
    public IReadOnlyList<CountLine> Approve()
    {
        if (Status != StocktakeStatus.Counting)
        {
            throw new DomainException("stocktake_nothing_counted", $"Stocktake {Id} has no recorded counts to approve.");
        }

        Status = StocktakeStatus.Approved;
        ApprovedAt = DateTimeOffset.UtcNow;
        var differences = _lines.Where(l => l.HasDifference).ToArray();
        Raise(new StocktakeApproved(Id, _lines.Count, differences.Length, DateTimeOffset.UtcNow));
        return differences;
    }

    public void Cancel()
    {
        if (Status == StocktakeStatus.Approved)
        {
            throw new DomainException("stocktake_already_approved", $"Stocktake {Id} was already approved.");
        }

        Status = StocktakeStatus.Cancelled;
    }
}

/// <summary>One counted position: what was found vs what the system expected.</summary>
public sealed record CountLine(
    LocationCode Location,
    Sku Sku,
    BatchNumber? Batch,
    Quantity Counted,
    Quantity Expected,
    string CountedBy)
{
    public bool HasDifference => Counted != Expected;
}
