using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.Users.GetLoggedInUser;

public sealed class LoggedInUserResponse
{
    public Guid Id { get; init; }

    public required string Email { get; init; }

    public required string Username { get; init; }

    public UserRole Role { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public string? Name { get; init; }

    public enSubscriptionPlan SubscriptionPlan { get; init; }

    public bool IsEmailConfirmed { get; init; }
}