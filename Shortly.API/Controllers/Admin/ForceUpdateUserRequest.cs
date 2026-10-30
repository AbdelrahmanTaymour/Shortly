using Shortly.Domain.Enums;
using Shortly.Domain.Users;

namespace Shortly.API.Controllers.Admin;

public sealed record ForceUpdateUserRequest(
    string? Email, string? Username, UserRole? Role, enSubscriptionPlan? SubscriptionPlanId, bool? IsActive,
    string? ProfileName, string? ProfileBio, string? ProfilePhoneNumber,
    string? ProfileCompany, string? ProfileLocation, string? ProfileCountry, string? ProfileTimeZone);