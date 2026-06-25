using Warehouse.Warehousing.Topology.Application.Warehouses.AddDock;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddLocation;
using Warehouse.Warehousing.Topology.Application.Warehouses.AddRoom;
using Warehouse.Warehousing.Topology.Application.Warehouses.ChangeRoomEnvironment;
using Warehouse.Warehousing.Topology.Application.Tree.GetLocations;
using Warehouse.Warehousing.Topology.Application.Tree.GetRoom;
using Warehouse.Warehousing.Topology.Application.Tree.GetTopologyTree;
using Warehouse.Warehousing.Topology.Application.Warehouses.ChangeLocationCapacity;
using Warehouse.Warehousing.Topology.Application.Warehouses.EstablishWarehouse;
using Warehouse.Warehousing.Topology.Application.Warehouses.GetWarehouse;
using Warehouse.Warehousing.Topology.Application.Warehouses.ListWarehouses;

namespace Warehouse.Warehousing.Api;

/// <summary>
/// Warehouse topology HTTP surface (UC-14). Thin: each route maps the request onto a use-case
/// command/query and delegates to its handler. The warehouse is the aggregate, so rooms, locations and
/// docks are addressed under it. Domain failures are translated centrally by <see cref="DomainExceptionHandler"/>.
/// </summary>
internal static class TopologyEndpoints
{
    public static IEndpointRouteBuilder MapTopologyEndpoints(this IEndpointRouteBuilder app)
    {
        var warehouses = app.MapGroup("/topology/warehouses");

        // Establish a new warehouse site.
        warehouses.MapPost("/", async (
            EstablishWarehouseCommand command, EstablishWarehouseHandler handler, CancellationToken ct) =>
        {
            var code = await handler.HandleAsync(command, ct);
            return Results.Created($"/topology/warehouses/{code}", new { code });
        });

        // List all sites with structural counts.
        warehouses.MapGet("/", async (ListWarehousesHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListWarehousesQuery(), ct)));

        // Read one site (rooms, locations, docks).
        warehouses.MapGet("/{code}", async (string code, GetWarehouseHandler handler, CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(new GetWarehouseQuery(code), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // Add a room to a site.
        warehouses.MapPost("/{code}/rooms", async (
            string code, AddRoomRequest request, AddRoomHandler handler, CancellationToken ct) =>
        {
            var room = await handler.HandleAsync(
                new AddRoomCommand(
                    code, request.Code, request.Type, request.MinCelsius, request.MaxCelsius, request.HumidityControlled),
                ct);
            return Results.Created($"/topology/warehouses/{code}", new { room });
        });

        // Add a storage location to a room.
        warehouses.MapPost("/{code}/rooms/{room}/locations", async (
            string code, string room, AddLocationRequest request, AddLocationHandler handler, CancellationToken ct) =>
        {
            var location = await handler.HandleAsync(
                new AddLocationCommand(
                    code, room, request.Code, request.Kind, request.CapacityM3, request.MaxLoadKg),
                ct);
            return Results.Created($"/topology/warehouses/{code}", new { location });
        });

        // Re-tune a room's maintained environment.
        warehouses.MapPost("/{code}/rooms/{room}/environment", async (
            string code, string room, ChangeRoomEnvironmentRequest request,
            ChangeRoomEnvironmentHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(
                new ChangeRoomEnvironmentCommand(
                    code, room, request.MinCelsius, request.MaxCelsius, request.HumidityControlled),
                ct);
            return Results.NoContent();
        });

        // Add a dock (ramp) to a site.
        warehouses.MapPost("/{code}/docks", async (
            string code, AddDockRequest request, AddDockHandler handler, CancellationToken ct) =>
        {
            var dock = await handler.HandleAsync(new AddDockCommand(code, request.Code, request.Direction), ct);
            return Results.Created($"/topology/warehouses/{code}", new { dock });
        });

        // Admin read model (UC-14): the flat topology tree the desk renders, and a room's detail. The room
        // id is the tree node id ("{warehouseCode}:{roomCode}"), so the screen round-trips it opaquely.
        app.MapGet("/topology/tree", async (GetTopologyTreeHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)));

        app.MapGet("/topology/room/{id}", async (string id, GetRoomHandler handler, CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(new GetRoomQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // Flat location list for the gateway's global-search BFF.
        app.MapGet("/topology/locations", async (GetLocationsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)));

        // Admin write model (UC-14), addressed by the flat tree ids the screen holds: a room is
        // "{warehouseCode}:{roomCode}". These thin routes split that id back out and delegate to the
        // warehouse-scoped use-case handlers, so the desk never has to know the aggregate boundary.

        // Add a room — returns the new room's tree node id so the screen can select it.
        app.MapPost("/topology/rooms", async (
            AddRoomFlatRequest request, AddRoomHandler handler, CancellationToken ct) =>
        {
            var room = await handler.HandleAsync(
                new AddRoomCommand(
                    request.Warehouse, request.Code, RoomTypeName(request.Type),
                    request.TempMin, request.TempMax, HumidityControlled: false),
                ct);
            var id = $"{request.Warehouse}:{room}";
            return Results.Created($"/topology/room/{id}", new { id });
        });

        // Re-tune a room's environment (the room type is fixed, so the FE's type field is display-only here).
        app.MapPost("/topology/room/{id}", async (
            string id, SaveRoomFlatRequest request, ChangeRoomEnvironmentHandler handler, CancellationToken ct) =>
        {
            if (SplitRoomId(id) is not { } ids) return Results.NotFound();
            await handler.HandleAsync(
                new ChangeRoomEnvironmentCommand(
                    ids.Warehouse, ids.Room, request.TempMin, request.TempMax, HumidityControlled: false),
                ct);
            return Results.NoContent();
        });

        // Add a location to a room (defaults to a rack; the FE address is the location code).
        app.MapPost("/topology/room/{roomId}/locations", async (
            string roomId, AddLocationFlatRequest request, AddLocationHandler handler, CancellationToken ct) =>
        {
            if (SplitRoomId(roomId) is not { } ids) return Results.NotFound();
            var location = await handler.HandleAsync(
                new AddLocationCommand(ids.Warehouse, ids.Room, request.Address, "Rack", request.Capacity, request.LoadLimit), ct);
            return Results.Created($"/topology/room/{roomId}", new { location });
        });

        // Re-rate a location's capacity / load limit.
        app.MapPost("/topology/room/{roomId}/location/{locationId}", async (
            string roomId, string locationId, EditLocationFlatRequest request,
            ChangeLocationCapacityHandler handler, CancellationToken ct) =>
        {
            if (SplitRoomId(roomId) is not { } ids) return Results.NotFound();
            await handler.HandleAsync(
                new ChangeLocationCapacityCommand(ids.Warehouse, ids.Room, locationId, request.Capacity, request.LoadLimit), ct);
            return Results.NoContent();
        });

        return app;
    }

    /// <summary>Split a flat room node id ("{warehouseCode}:{roomCode}") into its parts; null when the id
    /// is not a room node.</summary>
    private static (string Warehouse, string Room)? SplitRoomId(string id)
    {
        var i = id.IndexOf(':');
        return i <= 0 || i == id.Length - 1 ? null : (id[..i], id[(i + 1)..]);
    }

    /// <summary>Translate the FE room-type key ("cold"/"freezer"/"hazmat"/"standard") to the domain enum
    /// name the <see cref="AddRoomCommand"/> parses.</summary>
    private static string RoomTypeName(string feType) => feType switch
    {
        "cold" => "ColdRoom",
        "freezer" => "Freezer",
        "hazmat" => "HazmatZone",
        _ => "Standard",
    };
}

// Flat write payloads the admin Topology screen sends (addressed by tree node ids, not warehouse codes).
internal sealed record AddRoomFlatRequest(string Code, string Warehouse, string Type, decimal? TempMin, decimal? TempMax);

internal sealed record SaveRoomFlatRequest(string? Type, decimal? TempMin, decimal? TempMax);

internal sealed record AddLocationFlatRequest(string Address, decimal Capacity, decimal LoadLimit);

internal sealed record EditLocationFlatRequest(string? Id, decimal Capacity, decimal LoadLimit);

internal sealed record AddRoomRequest(
    string Code, string Type, decimal? MinCelsius, decimal? MaxCelsius, bool HumidityControlled);

internal sealed record AddLocationRequest(string Code, string Kind, decimal CapacityM3, decimal MaxLoadKg);

internal sealed record ChangeRoomEnvironmentRequest(
    decimal? MinCelsius, decimal? MaxCelsius, bool HumidityControlled);

internal sealed record AddDockRequest(string Code, string Direction);
