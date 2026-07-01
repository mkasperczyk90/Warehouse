using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Inventory.Domain.Events;

namespace Warehouse.Warehousing.Inventory.Domain;

/// <summary>
/// Thing archetype (🟩): goods of one SKU from a single delivery/production run.
/// Carries the expiry date (FEFO) and the QC hold — a blocked batch is invisible
/// to reservations across all locations at once.
/// </summary>
public sealed class Batch : AggregateRoot<BatchId>
{
    private Batch(BatchId id, Sku sku, BatchNumber number, DateOnly? expiryDate, string? supplierRef)
        : base(id)
    {
        Sku = sku;
        Number = number;
        ExpiryDate = expiryDate;
        SupplierRef = supplierRef;
        Quality = QualityStatus.Released;
    }

    private Batch()
    {
    }

    public Sku Sku { get; private set; } = null!;

    public BatchNumber Number { get; private set; } = null!;

    public DateOnly? ExpiryDate { get; private set; }

    /// <summary>Reference to the inbound delivery / supplier document the batch came from.</summary>
    public string? SupplierRef { get; private set; }

    public QualityStatus Quality { get; private set; }

    public static Batch Register(Sku sku, BatchNumber number, DateOnly? expiryDate, string? supplierRef = null)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(number);
        return new Batch(BatchId.New(), sku, number, expiryDate, supplierRef);
    }

    public bool IsExpiredAt(DateOnly date) => ExpiryDate is { } expiry && expiry < date;

    public void Quarantine()
    {
        if (Quality == QualityStatus.Rejected)
        {
            throw new DomainException("batch_already_rejected", $"Batch {Number} ({Sku}) was already rejected.");
        }

        if (Quality == QualityStatus.Quarantine)
        {
            return;
        }

        Quality = QualityStatus.Quarantine;
        Raise(new BatchQuarantined(Id, Sku, Number, DateTimeOffset.UtcNow));
    }

    public void Block(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Quality = QualityStatus.Rejected;
        Raise(new BatchBlocked(Id, Sku, Number, reason, DateTimeOffset.UtcNow));
    }

    public void Release()
    {
        if (Quality == QualityStatus.Released)
        {
            return;
        }

        Quality = QualityStatus.Released;
        Raise(new BatchReleased(Id, Sku, Number, DateTimeOffset.UtcNow));
    }
}
