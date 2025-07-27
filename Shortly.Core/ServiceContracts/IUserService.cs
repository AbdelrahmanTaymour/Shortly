using Shortly.Core.DTOs.UsersDTOs;

namespace Shortly.Core.ServiceContracts;

public interface IUserService
{
    // CRUD Operations
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest createUserRequest);
    
    
    // Advanced queries
    Task<IEnumerable<UserProfileDto>> GetAllUsersAsync(); 
}