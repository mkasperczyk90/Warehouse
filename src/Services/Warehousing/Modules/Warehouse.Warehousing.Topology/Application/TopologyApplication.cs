using Microsoft.Extensions.DependencyInjection;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddDock;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddLocation;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddRoom;
using Warehouse.Warehousing.Topology.Application.Warehouses.ChangeLocationCapacity;
using Warehouse.Warehousing.Topology.Application.Warehouses.ChangeRoomEnvironment;
using Warehouse.Warehousing.Topology.Application.Tree.GetLocations;
using Warehouse.Warehousing.Topology.Application.Tree.GetRoom;
using Warehouse.Warehousing.Topology.Application.Tree.GetTopologyTree;
using Warehouse.Warehousing.Topology.Application.Warehouses.EstablishWarehouse;
using Warehouse.Warehousing.Topology.Application.Warehouses.GetWarehouse;
using Warehouse.Warehousing.Topology.Application.Warehouses.ListWarehouses;

namespace Warehouse.Warehousing.Topology.Application;

/// <summary>
/// Registers the Topology use-case handlers (one per vertical slice, ADR-0007). Endpoints resolve these
/// directly; wired from <c>Program.cs</c> via <c>AddTopologyApplication()</c>.
/// </summary>
public static class TopologyApplication
{
    public static IServiceCollection AddTopologyApplication(this IServiceCollection services)
    {
        services.AddScoped<EstablishWarehouseHandler>();
        services.AddScoped<AddRoomHandler>();
        services.AddScoped<AddLocationHandler>();
        services.AddScoped<AddDockHandler>();
        services.AddScoped<ChangeRoomEnvironmentHandler>();
        services.AddScoped<ChangeLocationCapacityHandler>();
        services.AddScoped<GetWarehouseHandler>();
        services.AddScoped<ListWarehousesHandler>();

        // Admin read model: the flat topology tree + room detail the Topology screen renders.
        services.AddScoped<GetTopologyTreeHandler>();
        services.AddScoped<GetRoomHandler>();
        services.AddScoped<GetLocationsHandler>();
        return services;
    }
}
