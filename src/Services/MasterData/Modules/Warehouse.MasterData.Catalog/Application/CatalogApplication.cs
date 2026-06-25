using Microsoft.Extensions.DependencyInjection;
using Warehouse.MasterData.Catalog.Application.Products.ChangeProductStorage;
using Warehouse.MasterData.Catalog.Application.Products.DefineProduct;
using Warehouse.MasterData.Catalog.Application.Products.GetProduct;
using Warehouse.MasterData.Catalog.Application.Products.ImportProducts;
using Warehouse.MasterData.Catalog.Application.Products.ListProducts;
using Warehouse.MasterData.Catalog.Application.Products.RenameProduct;

namespace Warehouse.MasterData.Catalog.Application;

/// <summary>
/// Registers the Catalog use-case handlers (one per vertical slice, ADR-0007). Endpoints resolve these
/// directly; wired from <c>Program.cs</c> via <c>AddCatalogApplication()</c>.
/// </summary>
public static class CatalogApplication
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<DefineProductHandler>();
        services.AddScoped<ImportProductsHandler>();
        services.AddScoped<RenameProductHandler>();
        services.AddScoped<ChangeProductStorageHandler>();
        services.AddScoped<GetProductHandler>();
        services.AddScoped<ListProductsHandler>();
        return services;
    }
}
