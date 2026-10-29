using Shortly.Domain.Enums;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.GetUserDetail;

public sealed record UserAdminDetailResponse(
    Guid Id, 
    string Email,
    string Username, 
    UserRole Role, 
    enSubscriptionPlan EnSubscriptionPlanId,
    bool IsActive, 
    bool IsEmailConfirmed, 
    bool IsOAuthUser, 
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastLoginAtUtc,
    bool IsDeleted, 
    DateTime? DeletedAtUtc,
    string? ProfileName, 
    string? ProfileBio, 
    string? ProfilePhoneNumber,
    string? ProfileCompany,
    string? ProfileLocation, 
    string? ProfileCountry,
    int FailedLoginAttempts,
    bool IsLockedOut, 
    DateTime? LockedUntilUtc,
    bool TwoFactorEnabled,
    int MonthlyLinksCreated, 
    int TotalLinksCreated, 
    int MonthlyQrCodesCreated, 
    int TotalQrCodesCreated);