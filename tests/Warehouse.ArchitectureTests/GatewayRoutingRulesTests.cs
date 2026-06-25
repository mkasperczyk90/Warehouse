using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Warehouse.ArchitectureTests;

/// <summary>
/// Guards the production edge: every resource group an API exposes must be reachable through the YARP
/// gateway. The admin/terminal front-ends call the gateway (the real seam — MSW only stands in for it,
/// ADR-0006), so a backend endpoint with no gateway route is a 404 in production that no in-process
/// test catches. Convention: an endpoint group is <c>app.MapGroup("/&lt;resource&gt;/…")</c> in a
/// <c>*.Api</c> project, and the gateway routes <c>/api/&lt;resource&gt;/{**catch-all}</c> to the cluster
/// named after the owning service (<c>warehousing</c>, <c>masterdata</c>, <c>logistics</c>).
/// </summary>
public sealed partial class GatewayRoutingRulesTests
{
    private sealed record GatewayRoute(string Prefix, string ClusterId);

    private sealed record ApiGroup(string Prefix, string ExpectedCluster, string SourceFile);

    [Fact]
    public void Every_API_endpoint_group_is_routed_by_the_gateway_to_its_service_cluster()
    {
        var gateway = GatewayRoutes().ToDictionary(r => r.Prefix, r => r.ClusterId);

        var unmatched = ApiGroups()
            .Where(g => !gateway.TryGetValue(g.Prefix, out var cluster) || cluster != g.ExpectedCluster)
            .Select(g => gateway.TryGetValue(g.Prefix, out var cluster)
                ? $"'/{g.Prefix}' ({g.SourceFile}) is routed to cluster '{cluster}', expected '{g.ExpectedCluster}'"
                : $"'/{g.Prefix}' ({g.SourceFile}) has no gateway route (expected '/api/{g.Prefix}/{{**catch-all}}' → '{g.ExpectedCluster}')")
            .ToList();

        Assert.True(unmatched.Count == 0,
            "API endpoint groups not correctly routed by the gateway:" + Environment.NewLine +
            string.Join(Environment.NewLine, unmatched));
    }

    [Fact]
    public void Every_gateway_route_follows_the_api_prefix_convention_and_targets_a_declared_cluster()
    {
        var root = RepoRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(GatewayConfigPath(root)));
        var proxy = doc.RootElement.GetProperty("ReverseProxy");
        var clusters = proxy.GetProperty("Clusters").EnumerateObject().Select(c => c.Name).ToHashSet();

        var problems = new List<string>();
        foreach (var route in proxy.GetProperty("Routes").EnumerateObject())
        {
            var name = route.Name;
            var clusterId = route.Value.GetProperty("ClusterId").GetString();
            var path = route.Value.GetProperty("Match").GetProperty("Path").GetString() ?? "";

            if (!path.StartsWith("/api/", StringComparison.Ordinal) || !path.Contains("{**catch-all}"))
            {
                problems.Add($"route '{name}': path '{path}' does not follow '/api/<resource>/{{**catch-all}}'");
            }

            if (clusterId is null || !clusters.Contains(clusterId))
            {
                problems.Add($"route '{name}': targets unknown cluster '{clusterId}'");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>The gateway's declared routes as (resource-prefix, cluster) pairs, read from its config.</summary>
    private static IEnumerable<GatewayRoute> GatewayRoutes()
    {
        var root = RepoRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(GatewayConfigPath(root)));
        foreach (var route in doc.RootElement.GetProperty("ReverseProxy").GetProperty("Routes").EnumerateObject())
        {
            var clusterId = route.Value.GetProperty("ClusterId").GetString()!;
            var path = route.Value.GetProperty("Match").GetProperty("Path").GetString()!;
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && segments[0] == "api")
            {
                yield return new GatewayRoute(segments[1], clusterId);
            }
        }
    }

    /// <summary>Every <c>MapGroup("/…")</c> resource group declared in a <c>*.Api</c> project, paired with the
    /// cluster its owning service is expected to map to (service folder name, lower-cased).</summary>
    private static IEnumerable<ApiGroup> ApiGroups()
    {
        var root = RepoRoot();
        var services = Path.Combine(root, "src", "Services");
        foreach (var apiDir in Directory.GetDirectories(services, "Warehouse.*.Api", SearchOption.AllDirectories))
        {
            var service = Directory.GetParent(apiDir)!.Name;          // e.g. "Warehousing"
            var expectedCluster = service.ToLowerInvariant();         // gateway cluster id
            foreach (var file in Directory.GetFiles(apiDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                foreach (Match m in MapGroupPattern().Matches(File.ReadAllText(file)))
                {
                    var prefix = m.Groups[1].Value.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        yield return new ApiGroup(prefix, expectedCluster, Path.GetFileName(file));
                    }
                }
            }
        }
    }

    private static string GatewayConfigPath(string root)
    {
        var path = Path.Combine(root, "src", "Gateway", "Warehouse.Gateway", "appsettings.json");
        return File.Exists(path) ? path : throw new FileNotFoundException($"Gateway config not found at {path}.");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Warehouse.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate the repository root (Warehouse.slnx).");
    }

    [GeneratedRegex("MapGroup\\(\\s*\"(/[^\"]+)\"")]
    private static partial Regex MapGroupPattern();
}
