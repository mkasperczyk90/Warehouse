using Warehouse.SharedKernel.Domain;

namespace Warehouse.MasterData.Partners.Domain.Events;

/// <summary>Raised when a new business party is registered (before any role is assigned).</summary>
public sealed record PartyRegistered(PartyId PartyId, string Name, DateTimeOffset OccurredAt) : IDomainEvent;
