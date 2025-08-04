using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class UserMapper
{
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
            user.DeletedBy);
    }

    public static IEnumerable<UserDto> MapToUserProfileDtoList(this IEnumerable<User> users)
    {
        return users.Select(MapToUserDto);
    }
}