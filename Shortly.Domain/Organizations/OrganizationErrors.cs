using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Organizations;

public static class OrganizationErrors
{
    public static readonly Error NotAMember = Error.Forbidden(
        "Organization.NotAMember", "You are not an active member of this organization.");
}