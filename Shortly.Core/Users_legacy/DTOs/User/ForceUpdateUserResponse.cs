using Shortly.Core.Profile.DTOs;
using Shortly.Core.Security.DTOs;
using Shortly.Core.Users_legacy.DTOs.Usage;

namespace Shortly.Core.Users_legacy.DTOs.User;

public record ForceUpdateUserResponse(
    UserDto User,
    UserProfileResponse Profile,
    UserSecurityDto Security,
    UserUsageDto Usage
);