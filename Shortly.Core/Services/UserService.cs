using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;
using Shortly.Core.Mappers;
namespace Shortly.Core.Services;

public class UserService(IUserRepository userRepository): IUserService
{
    private readonly IUserRepository _userRepository = userRepository;

    public Task<CreateUserResponse> CreateUserAsync(CreateUserRequest createUserRequest)
    {
        throw new NotImplementedException();
    }

    
    // Advanced queries
    public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAll();
        return users.MapToUserProfileDtoList();
    }
}