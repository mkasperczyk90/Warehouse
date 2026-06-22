using Warehouse.Warehousing.Inventory.Application.Abstractions;
using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of the append-only ledger port (insert-only DbSet).</summary>
internal sealed class StockLedger(InventoryDbContext context) : IStockLedger
{
    public void Append(StockMovement movement) => context.StockMovements.Add(movement);
}
