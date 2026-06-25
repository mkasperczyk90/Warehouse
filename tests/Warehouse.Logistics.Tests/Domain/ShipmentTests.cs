using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Domain;

public sealed class ShipmentTests
{
    private static readonly PackageDimensions Carton = PackageDimensions.Of(40, 30, 20);

    private static Shipment NewShipment() =>
        Shipment.CreateFor(OrderId.New(), new PartyRoleRef(Guid.NewGuid().ToString()));

    [Fact]
    public void CreateFor_starts_in_Packing_for_the_carrier()
    {
        var carrier = new PartyRoleRef(Guid.NewGuid().ToString());

        var shipment = Shipment.CreateFor(OrderId.New(), carrier);

        Assert.Equal(ShipmentStatus.Packing, shipment.Status);
        Assert.Equal(carrier, shipment.Carrier);
        Assert.Empty(shipment.Packages);
    }

    [Fact]
    public void AddPackage_numbers_packages_from_one()
    {
        var shipment = NewShipment();

        shipment.AddPackage(Weight.FromKilograms(5), Carton);
        shipment.AddPackage(Weight.FromKilograms(3), Carton);

        Assert.Equal([1, 2], shipment.Packages.Select(p => p.Number));
    }

    [Fact]
    public void MarkReadyForPickup_requires_at_least_one_package()
    {
        var shipment = NewShipment();

        Expect.DomainError("shipment_empty", shipment.MarkReadyForPickup);
    }

    [Fact]
    public void Cannot_add_a_package_after_ready_for_pickup()
    {
        var shipment = NewShipment();
        shipment.AddPackage(Weight.FromKilograms(5), Carton);
        shipment.MarkReadyForPickup();

        Expect.DomainError("shipment_invalid_status", () => shipment.AddPackage(Weight.FromKilograms(1), Carton));
    }

    [Fact]
    public void Dispatch_requires_ready_for_pickup()
    {
        var shipment = NewShipment();
        shipment.AddPackage(Weight.FromKilograms(5), Carton); // still Packing

        Expect.DomainError("shipment_invalid_status", shipment.Dispatch);
    }

    [Fact]
    public void Dispatch_completes_the_shipment_and_raises_event_with_tracking()
    {
        var shipment = NewShipment();
        shipment.AddPackage(Weight.FromKilograms(5), Carton);
        shipment.MarkReadyForPickup();
        shipment.AssignTracking(TrackingNumber.Of("1Z-999"));

        shipment.Dispatch();

        Assert.Equal(ShipmentStatus.Dispatched, shipment.Status);
        Assert.NotNull(shipment.DispatchedAt);
        var dispatched = Assert.Single(shipment.DomainEvents.OfType<ShipmentDispatched>());
        Assert.Equal("1Z-999", dispatched.Tracking?.Value);
    }

    [Fact]
    public void Package_dimensions_must_be_positive()
    {
        Expect.DomainError("package_dimensions_invalid", () => PackageDimensions.Of(0, 30, 20));
    }
}
