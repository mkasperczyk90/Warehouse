using Warehouse.MasterData.Catalog.Application.Products.DefineProduct;
using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Catalog.Application.Products.ImportProducts;

/// <summary>
/// Bulk master-data import (UC-13): define many products in one request, typically from a CSV the desk
/// uploaded. Each row is the same primitive shape as <see cref="DefineProductCommand"/>.
/// </summary>
public sealed record ImportProductsCommand(IReadOnlyList<DefineProductCommand> Products);

/// <summary>One rejected row, keyed back to its 1-based position in the uploaded list so the desk can
/// fix the offending line. <see cref="Code"/> is the stable domain/validation code; <see cref="Message"/>
/// is the human-readable reason.</summary>
public sealed record ImportProductRowError(int Row, string Sku, string Code, string Message);

/// <summary>Per-row outcome: how many products were created and which rows failed and why. The import
/// itself always succeeds (HTTP 200) — partial failure is the normal case for a hand-edited file.</summary>
public sealed record ImportProductsResult(int Created, IReadOnlyList<ImportProductRowError> Failed);

/// <summary>
/// Imports products by replaying each row through <see cref="DefineProductHandler"/>, so every row goes
/// through the exact same registration rules and outbox announcement as a single define. Rows are
/// isolated: a bad row is collected as an <see cref="ImportProductRowError"/> and the rest continue, so a
/// hand-edited CSV with a few mistakes still lands the good rows. Good rows commit (and publish) as they
/// go — duplicate-SKU detection therefore also catches duplicates *within* the same file.
/// </summary>
public sealed class ImportProductsHandler(DefineProductHandler define)
{
    public async Task<ImportProductsResult> HandleAsync(
        ImportProductsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var failed = new List<ImportProductRowError>();
        var created = 0;

        for (var i = 0; i < command.Products.Count; i++)
        {
            var row = command.Products[i];
            try
            {
                await define.HandleAsync(row, cancellationToken);
                created++;
            }
            catch (DomainException ex)
            {
                failed.Add(new ImportProductRowError(i + 1, row.Sku, ex.ErrorCode, ex.Message));
            }
            catch (ArgumentException ex)
            {
                // Value-object guards (blank name, non-positive dimensions, …) throw ArgumentException
                // rather than DomainException; in an import these are just bad rows, not a server fault.
                failed.Add(new ImportProductRowError(i + 1, row.Sku, "invalid_input", ex.Message));
            }
        }

        return new ImportProductsResult(created, failed);
    }
}
