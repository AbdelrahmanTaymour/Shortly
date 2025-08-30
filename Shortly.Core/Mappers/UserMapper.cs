using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class UserMapper
{
    // Users
    public static UserDto MapToUserDto(this User user)
    {
        return new UserDto(user.Id,
            user.Email,
            user.Username,
            user.SubscriptionPlanId,
            user.Permissions,
            user.IsActive,
            user.IsEmailConfirmed,
            user.UpdatedAt,
            user.CreatedAt,
            user.IsDeleted,
            user.DeletedAt,
            user.DeletedBy
        );
    }

    public static IEnumerable<UserDto> MapToUserProfileDtoList(this IEnumerable<User> users)
    {
        return users.Select(MapToUserDto);
    }

    public static CreateUserResponse MapToCreateUserResponse(this User user)
    {
        return new CreateUserResponse(user.Id, user.Email, user.Username, user.SubscriptionPlanId, user.Permissions,
            user.IsActive, user.CreatedAt);
    }


    // Profiles
    public static UserProfileResponse MapToUserProfile(this UserProfile profile)
    {
        return new UserProfileResponse
        (
            profile.Name,
            profile.Bio,
            profile.PhoneNumber,
            profile.ProfilePictureUrl,
            profile.Website,
            profile.Company,
            profile.Location,
            profile.Country,
            profile.TimeZone,
            profile.UpdatedAt
        );
    }

    // Security
    public static UserSecurityDto MapToUserSecurityDto(this UserSecurity security)
    {
        return new UserSecurityDto
        (
            security.FailedLoginAttempts,
            security.LockedUntil,
            security.TwoFactorEnabled,
            security.TwoFactorSecret,
            security.UpdatedAt
        );
    }

    // Usage
    public static UserUsageDto MapToUserUsageDto(this UserUsage usage)
    {
        return new UserUsageDto
        (
            usage.MonthlyLinksCreated,
            usage.MonthlyQrCodesCreated,
            usage.TotalLinksCreated,
            usage.TotalQrCodesCreated,
            usage.MonthlyResetDate
        );
    }

    public static IEnumerable<UserUsageDto> MapToUserUsageDtoList(this IEnumerable<UserUsage> usages)
    {
        return usages.Select(MapToUserUsageDto);
    }
}