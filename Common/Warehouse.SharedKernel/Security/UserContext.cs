using Microsoft.AspNetCore.Http;

namespace Warehouse.SharedKernel.Security;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
	public string Username => httpContextAccessor.HttpContext?.User?.Claims
		.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? "System";

	public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
