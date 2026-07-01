using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): picking tasks for one outbound order, sequenced to
/// minimize travel (UC-10). Task locations/batches come from Inventory's reservations.
/// </summary>
public sealed class PickList : AggregateRoot<PickListId>
{
    private readonly List<PickTask> _tasks = [];

    private PickList(PickListId id, OrderId orderId) : base(id)
    {
        OrderId = orderId;
    }

    private PickList()
    {
    }

    public OrderId OrderId { get; private set; }

    public IReadOnlyCollection<PickTask> Tasks => _tasks.AsReadOnly();

    public bool IsCompleted => _tasks.Count > 0 && _tasks.All(t => t.Status != PickTaskStatus.Pending);

    public static PickList CreateFor(
        OrderId orderId,
        IReadOnlyCollection<(LocationRef Location, ProductCode Product, BatchInfo? Batch, Quantity Quantity)> plannedPicks)
    {
        if (plannedPicks is null || plannedPicks.Count == 0)
        {
            throw new DomainException("picklist_empty", "A pick list needs at least one task.");
        }

        var list = new PickList(PickListId.New(), orderId);
        var sequence = 1;
        foreach (var (location, product, batch, quantity) in plannedPicks)
        {
            list._tasks.Add(new PickTask(sequence++, location, product, batch, quantity));
        }

        return list;
    }

    public void ConfirmPick(int sequence, string pickedBy)
    {
        GetPendingTask(sequence).MarkPicked(pickedBy);
    }

    /// <summary>The location turned out short — the saga replans this task from another location/batch.</summary>
    public void ReportShort(int sequence, string reportedBy)
    {
        GetPendingTask(sequence).MarkShort(reportedBy);
    }

    private PickTask GetPendingTask(int sequence)
    {
        var task = _tasks.SingleOrDefault(t => t.Sequence == sequence)
            ?? throw new DomainException("pick_task_not_found", $"Pick list {Id} has no task {sequence}.");
        return task.Status == PickTaskStatus.Pending
            ? task
            : throw new DomainException("pick_task_not_pending", $"Task {sequence} of pick list {Id} is already {task.Status}.");
    }
}

public sealed class PickTask
{
    internal PickTask(int sequence, LocationRef location, ProductCode product, BatchInfo? batch, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(quantity);
        Sequence = sequence;
        Location = location;
        Product = product;
        Batch = batch;
        Quantity = quantity;
        Status = PickTaskStatus.Pending;
    }

    private PickTask()
    {
    }

    public int Sequence { get; }

    public LocationRef Location { get; } = null!;

    public ProductCode Product { get; } = null!;

    public BatchInfo? Batch { get; }

    public Quantity Quantity { get; } = null!;

    public PickTaskStatus Status { get; private set; }

    public string? HandledBy { get; private set; }

    internal void MarkPicked(string pickedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pickedBy);
        Status = PickTaskStatus.Picked;
        HandledBy = pickedBy;
    }

    internal void MarkShort(string reportedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportedBy);
        Status = PickTaskStatus.ShortPick;
        HandledBy = reportedBy;
    }
}
