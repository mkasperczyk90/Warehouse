using System.Net.Http.Json;
using System.Text.Json;

namespace Warehouse.Gateway.Bff;

/// <summary>
/// Shared HTTP fan-out for the BFF aggregators: resolves a named service client, forwards the active
/// warehouse, and reads a JSON list best-effort (a failing source returns empty rather than throwing, so
/// one slow service never fails the whole aggregate).
/// </summary>
public sealed class BffFetch(IHttpClientFactory httpClientFactory, ILogger<BffFetch> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HttpClient Client(string name, string? warehouseId)
    {
        var client = httpClientFactory.CreateClient(name);
        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            client.DefaultRequestHeaders.Add("X-Warehouse-Id", warehouseId);
        }

        return client;
    }

    public async Task<IReadOnlyList<T>> GetListAsync<T>(HttpClient client, string path, CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetFromJsonAsync<List<T>>(path, Json, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BFF source {Path} failed; section left empty.", path);
            return [];
        }
    }

    /// <summary>Read a single JSON object best-effort (a failing source returns <c>default</c>).</summary>
    public async Task<T?> GetAsync<T>(HttpClient client, string path, CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetFromJsonAsync<T>(path, Json, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BFF source {Path} failed; skipped.", path);
            return default;
        }
    }
}
