namespace Warehouse.Product.Api.Security;

public static class AuthorizationExtensions
{
	public static RouteHandlerBuilder RequireRole(this RouteHandlerBuilder builder, params string[] roles) => builder.RequireAuthorization(policy => policy.RequireRole(roles));
}
