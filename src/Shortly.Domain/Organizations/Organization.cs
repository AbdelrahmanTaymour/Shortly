using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Domain.Organizations;

public sealed class Organization : Entity<Guid>
{
    private Organization(Guid id, string name, string? description, string? website, string? logoUrl,
        int memberLimit, Guid ownerId, enSubscriptionPlan subscriptionPlanId, DateTime utcNow) : base(id)
    {
        Name = name;
        Description = description;
        Website = website;
        LogoUrl = logoUrl;
        MemberLimit = memberLimit;
        OwnerId = ownerId;
        SubscriptionPlanId = subscriptionPlanId;
        IsActive = true;
        IsSubscribed = subscriptionPlanId != enSubscriptionPlan.Free;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
        Usage = OrganizationUsage.CreateDefault(id, utcNow);
    }

    private Organization() { } // EF

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    public int MemberLimit { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSubscribed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid OwnerId { get; private set; }
    public enSubscriptionPlan SubscriptionPlanId { get; private set; }

    public bool IsDeleted => DeletedAt != null;

    public OrganizationUsage Usage { get; private set; } = null!;
    
    public static Result<Organization> Create(string name, string? description, string? website, string? logoUrl,
        int memberLimit, Guid ownerId, enSubscriptionPlan subscriptionPlanId, DateTime utcNow)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0) return Result.Failure<Organization>(OrganizationErrors.NameRequired);

        return new Organization(Guid.NewGuid(), normalizedName, description, website, logoUrl, memberLimit, ownerId,
            subscriptionPlanId, utcNow);
    }

    /// <summary>
    /// Updates the organization profile. <c>null</c> leaves a field unchanged; for the optional fields an empty
    /// (or whitespace-only) string clears the field. The name is trimmed and its inner whitespace collapsed.
    /// </summary>
    public Result UpdateDetails(string? name, string? description, string? website, string? logoUrl, int? memberLimit, DateTime utcNow)
    {
        string? normalizedName = null;
        if (name is not null)
        {
            normalizedName = NormalizeName(name);
            if (normalizedName.Length == 0) return Result.Failure(OrganizationErrors.NameRequired);
        }

        if (memberLimit is < 1) return Result.Failure(OrganizationErrors.InvalidMemberLimit);

        if (normalizedName is not null) Name = normalizedName;
        if (description is not null) Description = NullIfBlank(description);
        if (website is not null) Website = NullIfBlank(website);
        if (logoUrl is not null) LogoUrl = NullIfBlank(logoUrl);
        if (memberLimit.HasValue) MemberLimit = memberLimit.Value;

        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Activate(DateTime utcNow)
    {
        if (IsDeleted) return Result.Failure(OrganizationErrors.Deleted);
        if (IsActive) return Result.Failure(OrganizationErrors.AlreadyActive);
        IsActive = true;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Deactivate(DateTime utcNow)
    {
        if (!IsActive) return Result.Failure(OrganizationErrors.AlreadyInactive);
        IsActive = false;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result SoftDelete(DateTime utcNow)
    {
        if (IsDeleted) return Result.Failure(OrganizationErrors.AlreadyDeleted);
        DeletedAt = utcNow;
        IsActive = false;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result TransferOwnership(Guid newOwnerId, DateTime utcNow)
    {
        if (newOwnerId == Guid.Empty) return Result.Failure(OrganizationErrors.InvalidOwner);
        if (newOwnerId == OwnerId) return Result.Failure(OrganizationErrors.AlreadyOwner);
        OwnerId = newOwnerId;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result ChangeSubscriptionPlan(enSubscriptionPlan planId, DateTime utcNow)
    {
        if (!Enum.IsDefined(planId)) return Result.Failure(OrganizationErrors.InvalidSubscriptionPlan);
        SubscriptionPlanId = planId;
        IsSubscribed = planId != enSubscriptionPlan.Free;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    private static string NormalizeName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
