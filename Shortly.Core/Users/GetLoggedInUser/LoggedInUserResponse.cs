using Shortly.Domain.Enums;
using Shortly.Domain.Users;

namespace Shortly.Core.Users.GetLoggedInUser;

public sealed record LoggedInUserResponse(
    Guid Id, string Email, string Username, UserRole Role, string? ProfilePictureUrl, string? Name,
    enSubscriptionPlan SubscriptionPlanId, bool IsEmailConfirmed);
