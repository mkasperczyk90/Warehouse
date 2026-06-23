using Warehouse.SharedKernel.Domain;
using Xunit;

namespace Warehouse.SharedKernel.Tests.Domain;

public sealed class EntityTests
{
    private readonly record struct SampleId(Guid Value);

    private sealed class SampleEntity(SampleId id) : Entity<SampleId>(id);

    private sealed class OtherEntity(SampleId id) : Entity<SampleId>(id);

    [Fact]
    public void Entities_of_the_same_type_and_id_are_equal()
    {
        var id = new SampleId(Guid.NewGuid());

        var a = new SampleEntity(id);
        var b = new SampleEntity(id);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        Assert.NotEqual(new SampleEntity(new SampleId(Guid.NewGuid())), new SampleEntity(new SampleId(Guid.NewGuid())));
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        var id = new SampleId(Guid.NewGuid());

        Assert.False(new SampleEntity(id).Equals(new OtherEntity(id)));
    }

    [Fact]
    public void An_entity_never_equals_null()
    {
        Assert.False(new SampleEntity(new SampleId(Guid.NewGuid())).Equals(null));
    }
}

public sealed class AggregateRootTests
{
    private readonly record struct SampleId(Guid Value);

    private sealed record SampleEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class SampleAggregate() : AggregateRoot<SampleId>(new SampleId(Guid.NewGuid()))
    {
        public void DoSomething() => Raise(new SampleEvent(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_new_aggregate_has_no_domain_events()
    {
        Assert.Empty(new SampleAggregate().DomainEvents);
    }

    [Fact]
    public void Raise_records_a_domain_event()
    {
        var aggregate = new SampleAggregate();

        aggregate.DoSomething();

        Assert.Single(aggregate.DomainEvents);
    }

    [Fact]
    public void Dequeue_returns_the_events_and_clears_the_queue()
    {
        var aggregate = new SampleAggregate();
        aggregate.DoSomething();

        var dequeued = aggregate.DequeueDomainEvents();

        Assert.Single(dequeued);
        Assert.Empty(aggregate.DomainEvents);
    }
}

public sealed class DomainExceptionTests
{
    [Fact]
    public void Carries_the_error_code_and_message_it_was_given()
    {
        var ex = new DomainException("stock_insufficient", "not enough");

        Assert.Equal("stock_insufficient", ex.ErrorCode);
        Assert.Equal("not enough", ex.Message);
    }

    [Fact]
    public void Falls_back_to_a_default_error_code()
    {
        Assert.Equal("domain_rule_violated", new DomainException("just a message").ErrorCode);
        Assert.Equal("domain_rule_violated", new DomainException().ErrorCode);
    }
}
