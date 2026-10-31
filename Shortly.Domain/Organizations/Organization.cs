using Shortly.Domain.Abstractions;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Domain.Members;

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
    
    public static Organization Create(string name, string? description, string? website, string? logoUrl,
        int memberLimit, Guid ownerId, enSubscriptionPlan subscriptionPlanId, DateTime utcNow)
        => new(Guid.NewGuid(), name, description, website, logoUrl, memberLimit, ownerId, subscriptionPlanId, utcNow);

    public Result UpdateDetails(string? name, string? description, string? website, string? logoUrl, int? memberLimit, DateTime utcNow)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name)) return Result.Failure(OrganizationErrors.NameRequired);
            Name = name;
        }
        if (description is not null) Description = description;
        if (website is not null) Website = website;
        if (logoUrl is not null) LogoUrl = logoUrl;
        if (memberLimit.HasValue) MemberLimit = memberLimit.Value;

        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Activate(DateTime utcNow)
    {
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
        if (newOwnerId == OwnerId) return Result.Failure(OrganizationErrors.AlreadyOwner);
        OwnerId = newOwnerId;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result ChangeSubscriptionPlan(enSubscriptionPlan planId, DateTime utcNow)
    {
        SubscriptionPlanId = planId;
        IsSubscribed = planId != enSubscriptionPlan.Free;
        UpdatedAt = utcNow;
        return Result.Success();
    }
}