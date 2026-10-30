using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.GetOrganizationDetail;

public sealed record GetOrganizationDetailQuery(Guid OrganizationId) : IQuery<OrganizationDetailResponse>;
