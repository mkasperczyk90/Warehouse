using Warehouse.SharedKernel.Application;
using Warehouse.SharedKernel.Domain;
using Warehouse.SharedKernel.ValueObjects;
using Warehouse.Warehousing.Topology.Application.Abstractions;
using Warehouse.Warehousing.Topology.Domain;

namespace Warehouse.Warehousing.Topology.Application.Warehouses.EstablishWarehouse;

/// <summary>
/// Establish a new warehouse site (UC-14). Primitives only — the handler builds the value objects.
/// The code is unique across the topology; the address is required.
/// </summary>
public sealed record EstablishWarehouseCommand(
    string Code,
    string Name,
    string Street,
    string City,
    string PostalCode,
    string CountryCode);

/// <summary>
/// Registers a warehouse after the code-uniqueness check passes, then commits it as one transaction.
/// </summary>
public sealed class EstablishWarehouseHandler(IWarehouseRepository warehouses, IUnitOfWork unitOfWork)
{
    public async Task<string> HandleAsync(
        EstablishWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = WarehouseCode.Of(command.Code);
        if (await warehouses.ExistsAsync(code, cancellationToken))
        {
            throw new DomainException("warehouse_code_duplicate", $"Warehouse {code} already exists.");
        }

        var site = WarehouseSite.Establish(
            code,
            command.Name,
            Address.Of(command.Street, command.City, command.PostalCode, command.CountryCode));

        warehouses.Add(site);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return site.Code.Value;
    }
}
