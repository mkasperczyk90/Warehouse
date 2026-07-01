using System.Text.Json;
using Warehouse.Gateway.Auth;
using Xunit;

#pragma warning disable CA1861 // inline arrays are fine for one-off test fixtures

namespace Warehouse.Gateway.Tests;

public sealed class AuthClaimsTests
{
    [Fact]
    public void ToUser_reads_the_desk_user_from_the_token_claims()
    {
        var token = Jwt(new
        {
            sub = "u-1",
            badge = "1001",
            name = "K. Manager",
            email = "manager@warehouse.example",
            default_warehouse = "WH01",
            language = "en",
            realm_access = new { roles = new[] { "offline_access", "manager", "uma_authorization" } },
        });

        var user = AuthClaims.ToUser(token);

        Assert.Equal("u-1", user.Id);
        Assert.Equal("1001", user.Badge);
        Assert.Equal("K. Manager", user.Name);
        Assert.Equal("manager", user.Role);          // the desk role, not the default Keycloak roles
        Assert.Equal("manager@warehouse.example", user.Email);
        Assert.Equal("WH01", user.DefaultWarehouseId);
        Assert.Equal("en", user.Language);
    }

    [Fact]
    public void ToUser_falls_back_to_preferred_username_and_default_language()
    {
        var token = Jwt(new
        {
            sub = "u-2",
            badge = "1002",
            preferred_username = "1002",
            realm_access = new { roles = new[] { "coordinator" } },
        });

        var user = AuthClaims.ToUser(token);

        Assert.Equal("1002", user.Name);   // no `name` claim → preferred_username
        Assert.Equal("en", user.Language); // no `language` claim → default
        Assert.Equal("coordinator", user.Role);
    }

    [Fact]
    public void ToUser_reads_a_terminal_role_too()
    {
        var token = Jwt(new
        {
            sub = "u-7700",
            badge = "7700",
            name = "W. Operator",
            default_warehouse = "WH01",
            language = "en",
            realm_access = new { roles = new[] { "offline_access", "operator" } },
        });

        var user = AuthClaims.ToUser(token);

        Assert.Equal("operator", user.Role);   // handheld floor role, not just the desk roles
    }

    private static string Jwt(object payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        return $"{Segment("{\"alg\":\"none\"}"u8.ToArray())}.{Segment(json)}.sig";
    }

    private static string Segment(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
