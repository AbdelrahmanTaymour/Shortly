using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class UserMapper
{
    public static UserProfileDto MapToUserProfileDto(this User user)
    {
        return new UserProfileDto(
            user.Id,
            user.Name,
            user.Email,
            user.Username,
            user.SubscriptionPlan,
            user.Role,
            user.IsActive,
            user.IsEmailConfirmed,
            user.LastLoginAt,
            user.TimeZone,
            user.ProfilePictureUrl,
            user.MonthlyLinksCreated,
            user.TotalLinksCreated,
            user.MonthlyResetDate,
            user.TwoFactorEnabled,
            user.CreatedAt,
            user.UpdatedAt
        );
    }

    public static IEnumerable<UserProfileDto> MapToUserProfileDtoList(this IEnumerable<User> users)
    {
        return users.Select(MapToUserProfileDto);
    }

    public static UserDto MapToUserDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Username,
            user.SubscriptionPlan,
            user.Role,
            user.IsActive,
            user.IsDeleted,
            user.IsEmailConfirmed,
            user.LastLoginAt,
            user.TimeZone,
            user.ProfilePictureUrl,
            user.UpdatedAt,
            user.CreatedAt,
            user.DeletedAt,
            user.DeletedBy,
            user.MonthlyLinksCreated,
            user.TotalLinksCreated,
            user.MonthlyResetDate,
            user.FailedLoginAttempts,
            user.LockedUntil,
            user.TwoFactorEnabled,
            user.TwoFactorSecret
        );
    }

    public static IEnumerable<UserDto> MapToUserDtoList(this IEnumerable<User> users)
    {
        return users.Select(MapToUserDto);
    }

    public static CreateUserResponse MapToUserCreateResponse(this User user)
    {
        return new CreateUserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Username,
            user.SubscriptionPlan,
            user.Role,
            user.ProfilePictureUrl,
            user.TimeZone,
            user.IsActive,
            user.CreatedAt
        );
    }
}