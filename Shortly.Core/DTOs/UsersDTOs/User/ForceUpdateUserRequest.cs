using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.DTOs.UsersDTOs.Usage;

namespace Shortly.Core.DTOs.UsersDTOs.User;

public record ForceUpdateUserRequest(
    UserDto User,
    UserProfileResponse Profile,
    UserSecurityDto Security,
    UserUsageDto Usage
);