using Microsoft.EntityFrameworkCore;
using Warehouse.Logistics.Core.Domain;
using Warehouse.Logistics.Core.Infrastructure.Persistence;

namespace Warehouse.Logistics.Core.Application.GetDelivery;

/// <summary>Read model for a single inbound delivery (header + dock slot + lines).</summary>
public sealed record GetDeliveryQuery(Guid DeliveryId);

public sealed record DeliveryDto(
    Guid Id,
    Guid SupplierRoleId,
    string WarehouseCode,
    DateTimeOffset PlannedAt,
    string Status,
    DockSlotDto? Slot,
    IReadOnlyList<DeliveryLineDto> Lines);

public sealed record DockSlotDto(string DockCode, DateTimeOffset From, DateTimeOffset To);

public sealed record DeliveryLineDto(
    int LineNo,
    string ProductCode,
    decimal ExpectedQuantity,
    string ExpectedUnit,
    decimal? ActualQuantity,
    string? ActualUnit,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    string Discrepancy,
    string? Note);

public sealed class GetDeliveryHandler(LogisticsDbContext db)
{
    public async Task<DeliveryDto?> HandleAsync(GetDeliveryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var id = new DeliveryId(query.DeliveryId);

        var delivery = await db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return delivery is null ? null : Map(delivery);
    }

    internal static DeliveryDto Map(InboundDelivery d) => new(
        d.Id.Value,
        d.Supplier.Value,
        d.Warehouse.Code,
        d.PlannedAt,
        d.Status.ToString(),
        d.Slot is null ? null : new DockSlotDto(d.Slot.DockCode, d.Slot.From, d.Slot.To),
        d.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new DeliveryLineDto(
                l.LineNo,
                l.Product.Value,
                l.Expected.Amount,
                l.Expected.Unit.Code,
                l.Actual?.Amount,
                l.Actual?.Unit.Code,
                l.Batch?.Number,
                l.Batch?.ExpiryDate,
                l.Discrepancy.ToString(),
                l.Note))
            .ToList());
}
