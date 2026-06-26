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

    private Shipment(ShipmentId id, OrderId orderId) : base(id)
    {
        OrderId = orderId;
        Status = ShipmentStatus.AwaitingCarrier;
    }

    private Shipment()
    {
    }

    public OrderId OrderId { get; private set; }

    /// <summary>Set once a carrier is assigned (UC-12); null while the shipment awaits a carrier.</summary>
    public PartyRoleRef? Carrier { get; private set; }

    /// <summary>The booked pickup window/slot, recorded with the carrier assignment (free text in dev).</summary>
    public string? Pickup { get; private set; }

    public TrackingNumber? Tracking { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public IReadOnlyCollection<Package> Packages => _packages.AsReadOnly();

    /// <summary>Packed goods on the board, awaiting a carrier (created when the order is packed, UC-11).</summary>
    public static Shipment CreateAwaitingCarrier(OrderId orderId) => new(ShipmentId.New(), orderId);

    public Package AddPackage(Weight weight, PackageDimensions dimensions, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        EnsureStatus(ShipmentStatus.AwaitingCarrier, "add a package");
        var package = new Package(_packages.Count + 1, weight, dimensions, description);
        _packages.Add(package);
        return package;
    }

    /// <summary>Book a carrier and pickup slot — AwaitingCarrier → CarrierAssigned (UC-12).</summary>
    public void AssignCarrier(PartyRoleRef carrier, string pickup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pickup);
        EnsureStatus(ShipmentStatus.AwaitingCarrier, "assign a carrier");
        if (_packages.Count == 0)
        {
            throw new DomainException("shipment_empty", $"Shipment {Id} has no packages.");
        }

        Carrier = carrier;
        Pickup = pickup;
        Status = ShipmentStatus.CarrierAssigned;
    }

    /// <summary>Send the carrier its pickup notice — CarrierAssigned → ReadyForPickup (UC-12).</summary>
    public void SendPickupNotice()
    {
        EnsureStatus(ShipmentStatus.CarrierAssigned, "send a pickup notice");
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
        if (Carrier is not { } carrier)
        {
            throw new DomainException("shipment_no_carrier", $"Shipment {Id} has no carrier assigned.");
        }

        Status = ShipmentStatus.Dispatched;
        DispatchedAt = DateTimeOffset.UtcNow;
        Raise(new ShipmentDispatched(Id, OrderId, carrier, Tracking, DispatchedAt.Value));
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
    internal Package(int number, Weight weight, PackageDimensions dimensions, string? description)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        Number = number;
        Weight = weight;
        Dimensions = dimensions;
        Description = description;
    }

    private Package()
    {
    }

    public int Number { get; }

    public Weight Weight { get; }

    public PackageDimensions Dimensions { get; } = null!;

    public string? Description { get; }
}

/// <summary>Outer dimensions of a package in centimetres (UC-11 records weight and dimensions).</summary>
public sealed record PackageDimensions
{
    private PackageDimensions(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }

    public decimal LengthCm { get; }

    public decimal WidthCm { get; }

    public decimal HeightCm { get; }

    public static PackageDimensions Of(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        return lengthCm <= 0 || widthCm <= 0 || heightCm <= 0
            ? throw new DomainException(
                "package_dimensions_invalid",
                $"Package dimensions must be positive, got {lengthCm}×{widthCm}×{heightCm} cm.")
            : new PackageDimensions(lengthCm, widthCm, heightCm);
    }

    public override string ToString() => $"{LengthCm}×{WidthCm}×{HeightCm} cm";
}
