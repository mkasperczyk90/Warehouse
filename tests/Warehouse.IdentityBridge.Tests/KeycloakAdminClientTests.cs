using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Warehouse.IdentityBridge;
using Xunit;

namespace Warehouse.IdentityBridge.Tests;

/// <summary>
/// Exercises <see cref="KeycloakAdminClient"/> against a fake <see cref="HttpMessageHandler"/> — this
/// proves the client sends the requests it's supposed to and parses the responses it's supposed to,
/// for a Keycloak Admin REST API shape assumed from its long-stable, versioned documentation. It does
/// NOT prove a real Keycloak server actually behaves this way — unlike the CDC pipeline, this hasn't
/// been smoke-tested against a live container (docs/BI_Plan.md's Identity source note).
/// </summary>
public sealed class KeycloakAdminClientTests
{
    private static IOptions<IdentityBridgeOptions> Options() => Microsoft.Extensions.Options.Options.Create(
        new IdentityBridgeOptions { KeycloakBaseUrl = "http://keycloak.test", Realm = "warehouse" });

    [Fact]
    public async Task GetAccessTokenAsync_posts_the_password_grant_to_the_master_realm_and_parses_the_token()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(async request =>
        {
            captured = request;
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Equal(
                "grant_type=password&client_id=admin-cli&username=admin&password=admin",
                body);
            return JsonResponse("""{"access_token":"tok-123","expires_in":60}""");
        });
        var client = new KeycloakAdminClient(new HttpClient(handler), Options());

        var token = await client.GetAccessTokenAsync();

        Assert.Equal("tok-123", token);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("http://keycloak.test/realms/master/protocol/openid-connect/token", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListUsersAsync_sends_a_bearer_token_and_parses_the_user_list()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            captured = request;
            return Task.FromResult(JsonResponse("""
                [
                  {"id":"kc-1","username":"7700","firstName":"Jan","lastName":"Kowalski","enabled":true,
                   "attributes":{"default_warehouse":["WH01"]}}
                ]
                """));
        });
        var client = new KeycloakAdminClient(new HttpClient(handler), Options());

        var users = await client.ListUsersAsync("tok-123");

        var user = Assert.Single(users);
        Assert.Equal("7700", user.Username);
        Assert.Equal("WH01", user.Attributes!["default_warehouse"][0]);
        Assert.Equal("Bearer", captured!.Headers.Authorization!.Scheme);
        Assert.Equal("tok-123", captured.Headers.Authorization.Parameter);
        Assert.Equal("http://keycloak.test/admin/realms/warehouse/users?max=1000", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetRealmRolesAsync_parses_role_names_from_the_role_mappings_endpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            captured = request;
            return Task.FromResult(JsonResponse("""[{"id":"r1","name":"warehouse-operator"},{"id":"r2","name":"picker"}]"""));
        });
        var client = new KeycloakAdminClient(new HttpClient(handler), Options());

        var roles = await client.GetRealmRolesAsync("tok-123", "kc-1");

        Assert.Equal(["warehouse-operator", "picker"], roles);
        Assert.Equal(
            "http://keycloak.test/admin/realms/warehouse/users/kc-1/role-mappings/realm",
            captured!.RequestUri!.ToString());
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request);
    }
}
