using Warehouse.Contracts.Topology;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Domain.Replicas;

namespace Warehouse.Warehousing.Inventory.Application.Consumers;

/// <summary>
/// Projects Topology's location events into Inventory's local <see cref="LocationSnapshot"/> replica
/// (ADR-0003), so the put-away policy can validate environment + capacity without a cross-service query.
/// Both handlers are idempotent: a redelivered <c>LocationDefined</c> re-applies the same projection, and
/// a <c>RoomEnvironmentChanged</c> for a room with no replicated locations is a no-op (never throws, so a
/// duplicate is not dead-lettered).
/// </summary>
public sealed class LocationProjectionConsumer(ILocationSnapshotRepository locations, IUnitOfWork unitOfWork)
{
    public async Task Handle(LocationDefinedV1 message, CancellationToken cancellationToken)
    {
        var code = LocationCode.Of(message.Location);
        var temperature = TemperatureRange.Of(message.MinCelsius, message.MaxCelsius);
        var capacity = Volume.FromCubicMeters(message.CapacityM3);
        var maxLoad = Weight.FromKilograms(message.MaxLoadKg);

        var existing = await locations.FindAsync(code, cancellationToken);
        if (existing is null)
        {
            locations.Add(new LocationSnapshot(
                code,
                WarehouseCode.Of(message.Warehouse),
                message.Room,
                temperature,
                message.IsHazmatZone,
                capacity,
                maxLoad,
                message.OccurredAt));
        }
        else
        {
            existing.Apply(temperature, message.IsHazmatZone, capacity, maxLoad, message.OccurredAt);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(RoomEnvironmentChangedV1 message, CancellationToken cancellationToken)
    {
        var affected = await locations.ListByRoomAsync(
            WarehouseCode.Of(message.Warehouse), message.Room, cancellationToken);

        foreach (var snapshot in affected)
        {
            // A fresh range per snapshot: the owned value object is one EF entity per owner and may not
            // be shared by reference across rows, or EF nulls all but one out on save.
            snapshot.ApplyEnvironment(
                TemperatureRange.Of(message.MinCelsius, message.MaxCelsius), message.OccurredAt);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
