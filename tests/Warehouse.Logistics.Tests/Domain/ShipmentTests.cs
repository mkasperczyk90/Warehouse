using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Domain.Events;
using Warehouse.Logistics.Tests.TestDoubles;
using Warehouse.SharedKernel.ValueObjects;
using Xunit;

namespace Warehouse.Logistics.Tests.Domain;

public sealed class ShipmentTests
{
    private static readonly PackageDimensions Carton = PackageDimensions.Of(40, 30, 20);
    private static readonly PartyRoleRef Carrier = new("DH");

    private static Shipment Packed()
    {
        var shipment = Shipment.CreateAwaitingCarrier(OrderId.New());
        shipment.AddPackage(Weight.FromKilograms(5), Carton);
        return shipment;
    }

    [Fact]
    public void CreateAwaitingCarrier_starts_on_the_board_with_no_carrier()
    {
        var shipment = Shipment.CreateAwaitingCarrier(OrderId.New());

        Assert.Equal(ShipmentStatus.AwaitingCarrier, shipment.Status);
        Assert.Null(shipment.Carrier);
        Assert.Empty(shipment.Packages);
    }

    [Fact]
    public void AddPackage_numbers_packages_from_one()
    {
        var shipment = Shipment.CreateAwaitingCarrier(OrderId.New());

        shipment.AddPackage(Weight.FromKilograms(5), Carton);
        shipment.AddPackage(Weight.FromKilograms(3), Carton);

        Assert.Equal([1, 2], shipment.Packages.Select(p => p.Number));
    }

    [Fact]
    public void AssignCarrier_requires_at_least_one_package()
    {
        var shipment = Shipment.CreateAwaitingCarrier(OrderId.New());

        Expect.DomainError("shipment_empty", () => shipment.AssignCarrier(Carrier, "Tue 14:00"));
    }

    [Fact]
    public void AssignCarrier_books_the_carrier_and_pickup_slot()
    {
        var shipment = Packed();

        shipment.AssignCarrier(Carrier, "Tue 14:00");

        Assert.Equal(ShipmentStatus.CarrierAssigned, shipment.Status);
        Assert.Equal(Carrier, shipment.Carrier);
        Assert.Equal("Tue 14:00", shipment.Pickup);
    }

    [Fact]
    public void Cannot_add_a_package_after_a_carrier_is_assigned()
    {
        var shipment = Packed();
        shipment.AssignCarrier(Carrier, "Tue 14:00");

        Expect.DomainError("shipment_invalid_status", () => shipment.AddPackage(Weight.FromKilograms(1), Carton));
    }

    [Fact]
    public void Dispatch_requires_ready_for_pickup()
    {
        var shipment = Packed();
        shipment.AssignCarrier(Carrier, "Tue 14:00"); // CarrierAssigned, notice not yet sent

        Expect.DomainError("shipment_invalid_status", shipment.Dispatch);
    }

    [Fact]
    public void Full_lifecycle_dispatches_and_raises_event_with_tracking()
    {
        var shipment = Packed();
        shipment.AssignCarrier(Carrier, "Tue 14:00");
        shipment.SendPickupNotice();
        shipment.AssignTracking(TrackingNumber.Of("1Z-999"));

        shipment.Dispatch();

        Assert.Equal(ShipmentStatus.Dispatched, shipment.Status);
        Assert.NotNull(shipment.DispatchedAt);
        var dispatched = Assert.Single(shipment.DomainEvents.OfType<ShipmentDispatched>());
        Assert.Equal("1Z-999", dispatched.Tracking?.Value);
        Assert.Equal(Carrier, dispatched.Carrier);
    }

    [Fact]
    public void Package_dimensions_must_be_positive()
    {
        Expect.DomainError("package_dimensions_invalid", () => PackageDimensions.Of(0, 30, 20));
    }
}
