using Shortly.Core.Profile.DTOs;
using Shortly.Core.Security.DTOs;
using Shortly.Core.Users.DTOs.Usage;

namespace Shortly.Core.Users.DTOs.User;

public record ForceUpdateUserRequest(
    UserDto User,
    UserProfileResponse Profile,
    UserSecurityDto Security,
    UserUsageDto Usage
);