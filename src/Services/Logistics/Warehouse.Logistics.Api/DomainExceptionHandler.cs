using Microsoft.AspNetCore.Diagnostics;
using Warehouse.SharedKernel.Domain;

namespace Warehouse.Logistics.Api;

/// <summary>
/// Maps domain failures to HTTP the way the front end expects: a <see cref="DomainException"/>
/// becomes <c>409</c> with a stable <c>{ code, message }</c> body (the admin/terminal API seam reads
/// the code), and a missing aggregate becomes <c>404</c>.
/// </summary>
internal sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case DomainException domain:
                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                await httpContext.Response.WriteAsJsonAsync(
                    new { code = domain.ErrorCode, message = domain.Message }, cancellationToken);
                return true;

            case KeyNotFoundException notFound:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsJsonAsync(
                    new { code = "not_found", message = notFound.Message }, cancellationToken);
                return true;

            default:
                return false;
        }
    }
}
