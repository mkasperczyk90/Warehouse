using System.Reflection;
using System.Windows.Input;
using Warehouse.Inventory.Api;
using Warehouse.Inventory.Application;
using Warehouse.Inventory.Domain;
using Warehouse.Inventory.Infrastructure;

namespace Inventory.ArchitectureTests;

public abstract class BaseTest
{
	protected static readonly Assembly DomainAssembly = typeof(InventoryDomainMarker).Assembly;
	protected static readonly Assembly ApplicationAssembly = typeof(InventoryApplicationMarker).Assembly;
	protected static readonly Assembly InfrastructureAssembly = typeof(InventoryInfrastructureMarker).Assembly;
	protected static readonly Assembly PresentationAssembly = typeof(InventoryApiMarker).Assembly;
}
