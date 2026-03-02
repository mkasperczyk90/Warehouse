using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Inventory.Api.Security;
using Warehouse.Inventory.Api.Settings;

namespace Warehouse.Inventory.Api.Extensions;

public static class KeyCloakAuthenticationExtensions
{
	internal static void AddKeyCloakAuth(this IServiceCollection services, KeyCloakOptions keycloak) =>
		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.Authority = keycloak.Authority;
				options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
				options.MapInboundClaims = false;

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = keycloak.Authority,
					ValidateAudience = false,
					ValidateLifetime = true,
					NameClaimType = "preferred_username",
					RoleClaimType = ClaimTypes.Role
				};

				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = context =>
					{
						if (context.Principal?.Identity is ClaimsIdentity identity)
						{
							var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
							if (!string.IsNullOrEmpty(realmAccessClaim))
							{
								using var json = JsonDocument.Parse(realmAccessClaim);
								if (json.RootElement.TryGetProperty("roles", out var roles))
									foreach (var role in roles.EnumerateArray()) identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
							}
						}

						return Task.CompletedTask;
					}
				};
			});
}
