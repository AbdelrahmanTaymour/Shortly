using System.Runtime.CompilerServices;
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
}