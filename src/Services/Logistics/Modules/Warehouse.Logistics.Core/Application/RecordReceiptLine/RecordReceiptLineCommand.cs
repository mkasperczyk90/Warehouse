using Warehouse.Logistics.Core.Application.Abstractions;
using Warehouse.Logistics.Core.Domain;
using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.ValueObjects;

namespace Warehouse.Logistics.Core.Application.RecordReceiptLine;

/// <summary>UC-02 — record the counted quantity (and optional batch/discrepancy) for one line.</summary>
public sealed record RecordReceiptLineCommand(
    Guid DeliveryId,
    int LineNo,
    decimal Quantity,
    string Unit,
    string? BatchNumber = null,
    DateOnly? ExpiryDate = null,
    string Discrepancy = "None",
    string? Note = null);

public sealed class RecordReceiptLineHandler(IInboundDeliveryRepository deliveries, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(RecordReceiptLineCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var delivery = await deliveries.GetByIdAsync(new DeliveryId(command.DeliveryId), cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {command.DeliveryId} not found.");

        var actual = Quantity.Of(command.Quantity, UnitOfMeasure.FromCode(command.Unit));
        var batch = command.BatchNumber is { } number ? BatchInfo.Of(number, command.ExpiryDate) : null;
        var discrepancy = Enum.TryParse<DiscrepancyType>(command.Discrepancy, ignoreCase: true, out var parsed)
            ? parsed
            : DiscrepancyType.None;

        delivery.RecordReceipt(command.LineNo, actual, batch, discrepancy, command.Note);
        deliveries.Update(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
