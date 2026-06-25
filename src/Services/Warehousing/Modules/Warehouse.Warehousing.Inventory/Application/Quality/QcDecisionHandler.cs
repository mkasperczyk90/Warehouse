using Microsoft.EntityFrameworkCore;
using Warehouse.SharedKernel.Domain;
using Warehouse.Warehousing.Inventory.Domain;
using Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

namespace Warehouse.Warehousing.Inventory.Application.Quality;

/// <summary>
/// UC-03 — the inspector's verdict on a quarantined batch. Release lifts the hold (the batch and its held
/// stock become available again); reject blocks it (the batch is condemned, its stock stays out). The
/// decision is batch-level — it flips the <see cref="Batch"/> quality and every held <see cref="StockItem"/>
/// of that batch in one transaction. Status-only: no stock physically moves, so it writes no ledger entry.
/// </summary>
public sealed record QcDecisionCommand(Guid BatchId, string Decision, string Reason, string? Note);

public sealed class QcDecisionHandler(InventoryDbContext db)
{
    public async Task HandleAsync(QcDecisionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == new BatchId(command.BatchId), cancellationToken)
            ?? throw new DomainException("batch_not_found", $"Batch {command.BatchId} does not exist.");

        var release = command.Decision.Equals("release", StringComparison.OrdinalIgnoreCase);
        if (!release && !command.Decision.Equals("reject", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("qc_decision_invalid", $"Unknown QC decision '{command.Decision}'.");
        }

        var reason = string.IsNullOrWhiteSpace(command.Note) ? command.Reason : $"{command.Reason}: {command.Note}";

        // The held stock of this batch (value-converted columns, so filter the per-SKU set in memory).
        var held = (await db.StockItems.Where(s => s.Sku == batch.Sku).ToListAsync(cancellationToken))
            .Where(s => s.Batch == batch.Number && s.Status == StockStatus.Quarantine)
            .ToList();

        if (release)
        {
            batch.Release();
            held.ForEach(s => s.MarkAvailable());
        }
        else
        {
            batch.Block(reason);
            held.ForEach(s => s.MarkBlocked());
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
