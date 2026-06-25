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

    private Stocktake(StocktakeId id, IReadOnlyCollection<LocationCode> scope, string orderedBy, string label)
        : base(id)
    {
        Scope = scope;
        OrderedBy = orderedBy;
        Label = label;
        Status = StocktakeStatus.Open;
        StartedAt = DateTimeOffset.UtcNow;
    }

    private Stocktake()
    {
    }

    public IReadOnlyCollection<LocationCode> Scope { get; private set; } = [];

    /// <summary>Human description of what the count covers (e.g. "Cold room 1, aisle A") — for the worklist.</summary>
    public string Label { get; private set; } = null!;

    public string OrderedBy { get; private set; } = null!;

    public StocktakeStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public IReadOnlyCollection<CountLine> Lines => _lines.AsReadOnly();

    public static Stocktake Order(IReadOnlyCollection<LocationCode> scope, string orderedBy, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return scope is null || scope.Count == 0
            ? throw new DomainException("stocktake_scope_empty", "A stocktake needs at least one location to count.")
            : new Stocktake(StocktakeId.New(), scope, orderedBy, label);
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

/// <summary>One counted position: what was found vs what the system expected. A class (not a record) so
/// EF can materialize its owned <see cref="Quantity"/> values, which can't be constructor parameters.</summary>
public sealed class CountLine
{
    public CountLine(
        LocationCode location, Sku sku, BatchNumber? batch, Quantity counted, Quantity expected, string countedBy)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(counted);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentException.ThrowIfNullOrWhiteSpace(countedBy);
        Location = location;
        Sku = sku;
        Batch = batch;
        Counted = counted;
        Expected = expected;
        CountedBy = countedBy;
    }

    private CountLine()
    {
    }

    public LocationCode Location { get; private set; } = null!;

    public Sku Sku { get; private set; } = null!;

    public BatchNumber? Batch { get; private set; }

    public Quantity Counted { get; private set; } = null!;

    public Quantity Expected { get; private set; } = null!;

    public string CountedBy { get; private set; } = null!;

    public bool HasDifference => Counted != Expected;
}
