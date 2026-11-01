using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.GetUserDetail;

public sealed record UserAdminDetailResponse
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Username { get; init; }
    public UserRole Role { get; init; }
    public enSubscriptionPlan SubscriptionPlan { get; init; }
    public bool IsActive { get; init; }
    public bool IsEmailConfirmed { get; init; }
    public bool IsOAuthUser { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
    public string? ProfileName { get; init; }
    public string? ProfileBio { get; init; }
    public string? ProfilePhoneNumber { get; init; }
    public string? ProfileCompany { get; init; }
    public string? ProfileLocation { get; init; }
    public string? ProfileCountry { get; init; }
    public int FailedLoginAttempts { get; init; }
    public bool IsLockedOut { get; init; }
    public DateTime? LockedUntilUtc { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public int MonthlyLinksCreated { get; init; }
    public int TotalLinksCreated { get; init; }
    public int MonthlyQrCodesCreated { get; init; }
    public int TotalQrCodesCreated { get; init; }
}