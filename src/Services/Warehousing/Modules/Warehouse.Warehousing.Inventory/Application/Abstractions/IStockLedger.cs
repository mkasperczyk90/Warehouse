using Warehouse.Warehousing.Inventory.Domain;

namespace Warehouse.Warehousing.Inventory.Application.Abstractions;

/// <summary>
/// Append-only port for the stock movement ledger. Stock-changing behaviours on
/// <see cref="StockItem"/> return the <see cref="StockMovement"/> to persist; the application
/// appends it here so stock and ledger commit in the same transaction and can never diverge.
/// </summary>
public interface IStockLedger
{
    void Append(StockMovement movement);
}
