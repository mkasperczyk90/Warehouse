using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Domain;

public sealed class PickListTests
{
    private static PickList TwoTaskList()
    {
        var orderId = OrderId.New();
        return PickList.CreateFor(orderId,
        [
            (LocationRef.Of("L1"), Build.Code("A"), null, Build.Qty(2)),
            (LocationRef.Of("L2"), Build.Code("B"), BatchInfo.Of("LOT-1"), Build.Qty(4)),
        ]);
    }

    [Fact]
    public void CreateFor_sequences_tasks_and_raises_event()
    {
        var list = TwoTaskList();

        Assert.Equal([1, 2], list.Tasks.Select(t => t.Sequence));
        Assert.All(list.Tasks, t => Assert.Equal(PickTaskStatus.Pending, t.Status));
        var created = Assert.Single(list.DomainEvents.OfType<PickListCreated>());
        Assert.Equal(2, created.TaskCount);
    }

    [Fact]
    public void CreateFor_without_tasks_is_rejected()
    {
        Expect.DomainError("picklist_empty", () => PickList.CreateFor(OrderId.New(), []));
    }

    [Fact]
    public void ConfirmPick_marks_the_task_picked_with_who()
    {
        var list = TwoTaskList();

        list.ConfirmPick(1, "alice");

        var task = list.Tasks.Single(t => t.Sequence == 1);
        Assert.Equal(PickTaskStatus.Picked, task.Status);
        Assert.Equal("alice", task.HandledBy);
    }

    [Fact]
    public void ReportShort_marks_the_task_short()
    {
        var list = TwoTaskList();

        list.ReportShort(2, "bob");

        Assert.Equal(PickTaskStatus.ShortPick, list.Tasks.Single(t => t.Sequence == 2).Status);
    }

    [Fact]
    public void Confirming_an_unknown_sequence_is_rejected()
    {
        var list = TwoTaskList();

        Expect.DomainError("pick_task_not_found", () => list.ConfirmPick(99, "alice"));
    }

    [Fact]
    public void Confirming_an_already_handled_task_is_rejected()
    {
        var list = TwoTaskList();
        list.ConfirmPick(1, "alice");

        Expect.DomainError("pick_task_not_pending", () => list.ConfirmPick(1, "alice"));
    }

    [Fact]
    public void IsCompleted_is_false_while_a_task_is_pending_and_true_once_all_resolved()
    {
        var list = TwoTaskList();
        Assert.False(list.IsCompleted);

        list.ConfirmPick(1, "alice");
        Assert.False(list.IsCompleted);

        list.ReportShort(2, "bob"); // a short pick still counts as resolved
        Assert.True(list.IsCompleted);
    }
}
