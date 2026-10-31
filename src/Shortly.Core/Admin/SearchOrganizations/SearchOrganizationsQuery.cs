using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Admin.SearchOrganizations;

public sealed record SearchOrganizationsQuery(string? Search, int Page = 1, int PageSize = 25) 
    : IQuery<PagedResponse<OrganizationSummaryResponse>>;
