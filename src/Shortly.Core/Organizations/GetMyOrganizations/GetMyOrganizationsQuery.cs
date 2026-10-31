using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.GetMyOrganizations;

public sealed record GetMyOrganizationsQuery : IQuery<IReadOnlyList<MyOrganizationResponse>>;