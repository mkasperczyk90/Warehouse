using Microsoft.Extensions.DependencyInjection;
using Warehouse.MasterData.Partners.Application.Abstractions;
using Warehouse.MasterData.Partners.Infrastructure.Persistence;

namespace Warehouse.MasterData.Partners.Infrastructure;

/// <summary>Registers the Partners module's repositories. The <see cref="PartnersDbContext"/> is
/// registered by the host via Aspire's <c>AddNpgsqlDbContext</c>.</summary>
public static class PartnersInfrastructure
{
    public static IServiceCollection AddPartnersRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPartyRepository, PartyRepository>();
        return services;
    }
}
