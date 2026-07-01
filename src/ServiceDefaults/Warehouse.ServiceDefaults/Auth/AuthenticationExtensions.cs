using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.ServiceDefaults;

namespace Microsoft.Extensions.Hosting;

/// <summary>Shared Keycloak JWT wiring for the gateway (edge) and the backend services (zero-trust).</summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Validates Keycloak JWTs — signature + issuer + audience — against <c>Keycloak:Authority</c> (the
    /// resolved realm URL the AppHost injects; it must be host-reachable because JwtBearer's metadata
    /// backchannel does not run through service discovery), flattens realm roles to role claims, and
    /// registers the Desk / Terminal / Staff role policies. When
    /// <paramref name="requireAuthenticatedByDefault"/> is set (the backend services), every endpoint
    /// without explicit auth metadata additionally needs a valid warehouse token — the zero-trust floor,
    /// so a service validates the token itself instead of blindly trusting the gateway. Infra endpoints
    /// (<c>/health</c>, <c>/alive</c>, <c>/version</c>) are marked anonymous in <c>MapDefaultEndpoints</c>.
    /// </summary>
    public static TBuilder AddWarehouseJwtAuth<TBuilder>(this TBuilder builder, bool requireAuthenticatedByDefault = false)
        where TBuilder : IHostApplicationBuilder
    {
        var authority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/warehouse";
        var audience = builder.Configuration["Keycloak:ClientId"] ?? "warehouse-admin";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = audience;
            });

        // Keycloak carries realm roles under `realm_access.roles`; flatten them to role claims so the
        // policies below (and any RequireRole) can see them.
        builder.Services.AddSingleton<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

        var authorization = builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AppRoles.DeskPolicy, policy => policy.RequireRole(AppRoles.Desk))
            .AddPolicy(AppRoles.TerminalPolicy, policy => policy.RequireRole(AppRoles.Terminal))
            .AddPolicy(AppRoles.StaffPolicy, policy => policy.RequireRole(AppRoles.All));

        if (requireAuthenticatedByDefault)
        {
            // Zero-trust floor: any endpoint without explicit auth metadata needs an authenticated caller
            // holding one of the app's warehouse roles.
            authorization.SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireRole(AppRoles.All).Build());
        }

        return builder;
    }
}
