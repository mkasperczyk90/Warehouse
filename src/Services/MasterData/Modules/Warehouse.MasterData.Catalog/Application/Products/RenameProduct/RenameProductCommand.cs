using Warehouse.MasterData.Catalog.Application.Abstractions;
using Warehouse.MasterData.Catalog.Domain;
using Warehouse.SharedKernel.Application;

namespace Warehouse.MasterData.Catalog.Application.Products.RenameProduct;

/// <summary>Rename a product card. The SKU (identity) and EAN are immutable; only the label changes.</summary>
public sealed record RenameProductCommand(string Sku, string Name);

public sealed class RenameProductHandler(IProductTypeRepository products, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(RenameProductCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sku = Sku.Of(command.Sku);
        var product = await products.GetByIdAsync(sku, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {sku} was not found.");

        product.Rename(command.Name);
        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
