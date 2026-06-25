using Microsoft.Extensions.DependencyInjection;
using Warehouse.Warehousing.Inventory.Application.AdjustStock;
using Warehouse.Warehousing.Inventory.Application.AdjustStockStatus;
using Warehouse.Warehousing.Inventory.Application.ConfirmPutAway;
using Warehouse.Warehousing.Inventory.Application.MovementsLedger;
using Warehouse.Warehousing.Inventory.Application.ProposePutAway;
using Warehouse.Warehousing.Inventory.Application.Quality;
using Warehouse.Warehousing.Inventory.Application.StockOverview;
using Warehouse.Warehousing.Inventory.Application.Stocktakes;

namespace Warehouse.Warehousing.Inventory.Application;

/// <summary>
/// Registers the Inventory use-case handlers that endpoints invoke directly (put-away, the stock view,
/// and the desk's row actions). The integration-event consumers (ProductDefined, GoodsReceiptConfirmed)
/// are discovered by Wolverine.
/// </summary>
public static class InventoryApplication
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddScoped<ProposePutAwayHandler>();
        services.AddScoped<ConfirmPutAwayHandler>();
        services.AddScoped<StockOverviewHandler>();
        services.AddScoped<MovementsHandler>();
        services.AddScoped<MoveStockHandler>();
        services.AddScoped<BlockStockHandler>();
        services.AddScoped<AdjustStockHandler>();
        services.AddScoped<StocktakeQueries>();
        services.AddScoped<StartStocktakeHandler>();
        services.AddScoped<ApproveStocktakeHandler>();
        services.AddScoped<QcQueries>();
        services.AddScoped<QcDecisionHandler>();
        return services;
    }
}
