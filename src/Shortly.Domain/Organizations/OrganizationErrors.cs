using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Organizations;

public static class OrganizationErrors
{
    public static readonly Error NotFound = Error.NotFound("Organization.NotFound", "Organization not found.");
    public static readonly Error NotAMember = Error.Forbidden("Organization.NotAMember", "You are not a member of this organization.");
    public static readonly Error NotOwner = Error.Forbidden("Organization.NotOwner", "Only the organization owner can perform this action.");
    public static readonly Error InvalidSubscriptionPlan = Error.Validation("Organization.InvalidSubscriptionPlan", "The subscription plan is not valid.");
    public static readonly Error NameRequired = Error.Validation("Organization.NameRequired", "Organization name is required.");
    public static readonly Error AlreadyActive = Error.Conflict("Organization.AlreadyActive", "Organization is already active.");
    public static readonly Error AlreadyInactive = Error.Conflict("Organization.AlreadyInactive", "Organization is already inactive.");
    public static readonly Error AlreadyDeleted = Error.Conflict("Organization.AlreadyDeleted", "Organization is already deleted.");
    public static readonly Error Deleted = Error.Conflict("Organization.Deleted", "This organization has been deleted and can no longer be modified.");
    public static readonly Error InvalidOwner = Error.Validation("Organization.InvalidOwner", "A valid new owner is required.");
    public static readonly Error InvalidMemberLimit = Error.Validation("Organization.InvalidMemberLimit", "The member limit must be at least 1.");
    public static readonly Error OrganizationLimitReached = Error.Forbidden("Organization.OrganizationLimitReached", "Your plan does not allow you to own more organizations.");
    public static readonly Error AlreadyOwner = Error.Validation("Organization.AlreadyOwner", "This user is already the owner.");
    public static readonly Error NewOwnerNotMember = Error.Validation("Organization.NewOwnerNotMember", "The new owner must already be a member of this organization.");
    public static readonly Error Inactive = Error.Forbidden("Organization.Inactive", "This organization is inactive.");
    public static readonly Error UsageLimitExceeded = Error.Forbidden("Organization.UsageLimitExceeded", "Monthly usage limit exceeded.");
}