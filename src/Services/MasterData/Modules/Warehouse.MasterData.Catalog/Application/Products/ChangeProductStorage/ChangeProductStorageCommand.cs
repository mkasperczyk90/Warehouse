using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Catalog.Application.Products.ChangeProductStorage;

/// <summary>
/// Change a product's storage requirement (e.g. promote it to cold chain). The aggregate re-checks the
/// requirement against the product's category, so an incompatible change is rejected by the domain.
/// </summary>
public sealed record ChangeProductStorageCommand(string Sku, string Storage, decimal? MinCelsius, decimal? MaxCelsius);

public sealed class ChangeProductStorageHandler(IProductTypeRepository products, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(ChangeProductStorageCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sku = Sku.Of(command.Sku);
        var product = await products.GetByIdAsync(sku, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {sku} was not found.");

        product.ChangeStorageRequirement(
            ProductStorageMap.ToRequirement(command.Storage, command.MinCelsius, command.MaxCelsius));
        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
