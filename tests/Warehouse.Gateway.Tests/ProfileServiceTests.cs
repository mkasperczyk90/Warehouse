using System.Text.Json;
using Warehouse.Gateway.Bff;
using Xunit;

#pragma warning disable CA1861 // inline arrays are fine for one-off test fixtures

namespace Warehouse.Gateway.Tests;

public sealed class ProfileServiceTests
{
    [Fact]
    public void Build_shapes_the_profile_from_the_token_claims()
    {
        var service = new ProfileService();
        var token = Jwt(new
        {
            sub = "u-1",
            badge = "1001",
            name = "K. Manager",
            email = "manager@warehouse.example",
            phone = "+48 600 100 100",
            default_warehouse = "WH01",
            language = "en",
            auth_time = 1_750_000_000,
            realm_access = new { roles = new[] { "offline_access", "manager" } },
        });

        var profile = service.Build(token, "u-1");

        Assert.NotNull(profile);
        Assert.Equal("u-1", profile!.Id);
        Assert.Equal("1001", profile.Badge);
        Assert.Equal("K. Manager", profile.Name);
        Assert.Equal("manager", profile.Role);
        Assert.Equal("+48 600 100 100", profile.Phone);
        Assert.Equal("WH01", profile.DefaultWarehouseId);
        Assert.Equal("en", profile.Language);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$", profile.LastLogin);
        Assert.Single(profile.RecentSessions);
    }

    [Fact]
    public void Build_returns_null_when_asking_for_someone_elses_profile()
    {
        var service = new ProfileService();
        var token = Jwt(new { sub = "u-1", realm_access = new { roles = new[] { "manager" } } });

        Assert.Null(service.Build(token, "u-2"));
    }

    [Fact]
    public void Update_overlays_the_editable_prefs_and_Build_reflects_them()
    {
        var service = new ProfileService();
        var token = Jwt(new
        {
            sub = "u-1",
            phone = "+48 600 100 100",
            default_warehouse = "WH01",
            language = "en",
            realm_access = new { roles = new[] { "manager" } },
        });

        var updated = service.Update(token, "u-1", new ProfilePrefsDto("+48 999 888 777", "WH02", "pl"));

        Assert.NotNull(updated);
        Assert.Equal("+48 999 888 777", updated!.Phone);
        Assert.Equal("WH02", updated.DefaultWarehouseId);
        Assert.Equal("pl", updated.Language);

        // The overlay persists for the next read.
        var reread = service.Build(token, "u-1");
        Assert.Equal("WH02", reread!.DefaultWarehouseId);
        Assert.Equal("pl", reread.Language);
    }

    [Fact]
    public void Update_rejects_an_unknown_language_and_keeps_the_token_value()
    {
        var service = new ProfileService();
        var token = Jwt(new { sub = "u-1", language = "en", realm_access = new { roles = new[] { "manager" } } });

        var updated = service.Update(token, "u-1", new ProfilePrefsDto("", "WH01", "de"));

        Assert.Equal("en", updated!.Language); // unsupported "de" falls back to the token's language
    }

    private static string Jwt(object payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        return $"{Segment("{\"alg\":\"none\"}"u8.ToArray())}.{Segment(json)}.sig";
    }

    private static string Segment(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
