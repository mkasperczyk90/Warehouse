using Microsoft.Extensions.DependencyInjection;
using Warehouse.Warehousing.Inventory.Application.ConfirmPutAway;
using Warehouse.Warehousing.Inventory.Application.ProposePutAway;

namespace Warehouse.Warehousing.Inventory.Application;

/// <summary>
/// Registers the Inventory use-case handlers that endpoints invoke directly (put-away). The
/// integration-event consumers (ProductDefined, GoodsReceiptConfirmed) are discovered by Wolverine.
/// </summary>
public static class InventoryApplication
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddScoped<ProposePutAwayHandler>();
        services.AddScoped<ConfirmPutAwayHandler>();
        return services;
    }
}
