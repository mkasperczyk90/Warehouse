using System.Reflection;
using Warehouse.Product.Api;
using Warehouse.Product.Application;
using Warehouse.Product.Domain;
using Warehouse.Product.Infrastructure;

namespace Product.ArchitectureTests;

public abstract class BaseTest
{
	protected static readonly Assembly DomainAssembly = typeof(ProductDomainMarker).Assembly;
	protected static readonly Assembly ApplicationAssembly = typeof(ProductApplicationMarker).Assembly;
	protected static readonly Assembly InfrastructureAssembly = typeof(ProductInfrastructureMarker).Assembly;
	protected static readonly Assembly PresentationAssembly = typeof(ProductApiMarker).Assembly;
}
