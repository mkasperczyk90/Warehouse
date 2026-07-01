using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Domain;

/// <summary>
/// Moment-Interval archetype (🟨): packed goods handed over to a carrier (UC-11, UC-12).
/// </summary>
public sealed class Shipment : AggregateRoot<ShipmentId>
{
    private readonly List<Package> _packages = [];

    private Shipment(ShipmentId id, OrderId orderId, PartyRoleRef carrier) : base(id)
    {
        OrderId = orderId;
        Carrier = carrier;
        Status = ShipmentStatus.Packing;
    }

    private Shipment()
    {
    }

    public OrderId OrderId { get; private set; }

    public PartyRoleRef Carrier { get; private set; }

    public TrackingNumber? Tracking { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public IReadOnlyCollection<Package> Packages => _packages.AsReadOnly();

    public static Shipment CreateFor(OrderId orderId, PartyRoleRef carrier) =>
        new(ShipmentId.New(), orderId, carrier);

    public Package AddPackage(Weight weight, string? description = null)
    {
        EnsureStatus(ShipmentStatus.Packing, "add a package");
        var package = new Package(_packages.Count + 1, weight, description);
        _packages.Add(package);
        return package;
    }

    public void MarkReadyForPickup()
    {
        EnsureStatus(ShipmentStatus.Packing, "mark ready for pickup");
        if (_packages.Count == 0)
        {
            throw new DomainException("shipment_empty", $"Shipment {Id} has no packages.");
        }

        Status = ShipmentStatus.ReadyForPickup;
    }

    public void AssignTracking(TrackingNumber tracking)
    {
        ArgumentNullException.ThrowIfNull(tracking);
        Tracking = tracking;
    }

    /// <summary>Carrier collected the goods — Inventory deducts stock in reaction to the event.</summary>
    public void Dispatch()
    {
        EnsureStatus(ShipmentStatus.ReadyForPickup, "dispatch");
        Status = ShipmentStatus.Dispatched;
        DispatchedAt = DateTimeOffset.UtcNow;
        Raise(new ShipmentDispatched(Id, OrderId, Carrier, Tracking, DispatchedAt.Value));
    }

    private void EnsureStatus(ShipmentStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainException(
                "shipment_invalid_status",
                $"Cannot {action}: shipment {Id} is {Status}, expected {expected}.");
        }
    }
}

public sealed class Package
{
    internal Package(int number, Weight weight, string? description)
    {
        Number = number;
        Weight = weight;
        Description = description;
    }

    private Package()
    {
    }

    public int Number { get; }

    public Weight Weight { get; }

    public string? Description { get; }
}
