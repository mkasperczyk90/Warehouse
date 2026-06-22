using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Application.AnnounceDelivery;

/// <summary>
/// Validates every announced SKU against the local Catalog replica (ADR-0003 — no cross-service
/// query) and creates the <see cref="InboundDelivery"/>. An ASN that names an unknown SKU is
/// rejected so it never enters the receiving flow with a code Inventory cannot resolve.
/// </summary>
public sealed class AnnounceDeliveryHandler(
    IInboundDeliveryRepository deliveries,
    ICatalogProductReplica catalog,
    IUnitOfWork unitOfWork)
{
    public async Task<Guid> HandleAsync(AnnounceDeliveryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Lines is null || command.Lines.Count == 0)
        {
            throw new DomainException("delivery_lines_empty", "An ASN needs at least one line.");
        }

        var codes = command.Lines.Select(l => ProductCode.Of(l.ProductCode)).ToList();
        var unknown = await catalog.FindUnknownAsync(codes, cancellationToken);
        if (unknown.Count > 0)
        {
            throw new DomainException(
                "delivery_unknown_sku",
                $"Unknown SKU(s) not in the catalog: {string.Join(", ", unknown)}.");
        }

        var lines = command.Lines
            .Select(l =>
            {
                var product = ProductCode.Of(l.ProductCode);
                var expected = Quantity.Of(l.Quantity, UnitOfMeasure.FromCode(l.Unit));
                DeliveryPack? pack = l is { PackFactor: { } factor, PackUnit: { } packUnit }
                    ? DeliveryPack.Of(UnitOfMeasure.FromCode(packUnit), factor)
                    : null;
                return (product, expected, pack);
            })
            .ToList();

        var delivery = InboundDelivery.Announce(
            new PartyRoleRef(command.SupplierRoleId),
            WarehouseRef.Of(command.WarehouseCode),
            command.PlannedAt,
            lines);

        deliveries.Add(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return delivery.Id.Value;
    }
}
