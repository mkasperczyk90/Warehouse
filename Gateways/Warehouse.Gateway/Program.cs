using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.WithOrigins("http://localhost:3000") // Zmień na URL swojego frontendu
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
	{
		options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
	})
	.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
	{
		options.Cookie.Name = "__Gateway-Auth";
		options.Cookie.SameSite = SameSiteMode.Lax;
	})
	.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
	{
		options.Authority = builder.Configuration["Keycloak:Authority"];
		options.ClientId = builder.Configuration["Keycloak:ClientId"];
		options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
		options.ResponseType = OpenIdConnectResponseType.Code;

		options.SaveTokens = true;
		options.GetClaimsFromUserInfoEndpoint = true;
		options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = builder.Configuration["Keycloak:ValidIssuer"], // Ten sam adres co w API
			NameClaimType = "preferred_username",
			RoleClaimType = ClaimTypes.Role
		};

		options.Events = new OpenIdConnectEvents
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
						{
							foreach (var role in roles.EnumerateArray())
							{
								identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
							}
						}
					}
				}
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("Default", policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();
app.UseCors();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapReverseProxy(proxyPipeline =>
{
	proxyPipeline.Use(async (context, next) =>
	{
		var token = await context.GetTokenAsync("access_token");
		var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
		if (string.IsNullOrEmpty(token))
			logger.LogWarning("No token for path: {Path}", context.Request.Path);
		else
			context.Request.Headers.Authorization = $"Bearer {token}";
		await next();
	});
});

app.MapGet("/weatherforecast", () =>
	{
		return "forecast";
	})
	.WithName("GetWeatherForecast");

app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
{
	if (user.Identity?.IsAuthenticated == true)
	{
		return Results.Ok(new {
			Username = user.Identity.Name,
			Roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
		});
	}
	return Results.Unauthorized();
});

app.MapGet("/api/auth/login", (string returnUrl = "http://localhost:3000/") =>
	Results.Challenge(new AuthenticationProperties { RedirectUri = returnUrl }));

app.MapGet("/api/auth/logout", () =>
	Results.SignOut(new AuthenticationProperties { RedirectUri = "http://localhost:3000/" },
		[CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

app.Run();
