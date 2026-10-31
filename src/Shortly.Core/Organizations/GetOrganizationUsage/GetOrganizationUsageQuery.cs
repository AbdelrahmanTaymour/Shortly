using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.GetOrganizationUsage;

public sealed record GetOrganizationUsageQuery(Guid OrganizationId) : IQuery<OrganizationUsageResponse>;
