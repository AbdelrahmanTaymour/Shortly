using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Admin.GetUserDetail;
using Shortly.Domain.Enums;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.ForceUpdateUser;

public sealed record ForceUpdateUserCommand(
    Guid UserId,
    string? Email,
    string? Username,
    UserRole? Role,
    enSubscriptionPlan? SubscriptionPlanId,
    bool? IsActive,
    string? ProfileName,
    string? ProfileBio,
    string? ProfilePhoneNumber,
    string? ProfileCompany,
    string? ProfileLocation,
    string? ProfileCountry,
    string? ProfileTimeZone) : ICommand;