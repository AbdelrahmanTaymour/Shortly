using Shortly.Core.DTOs;

namespace Shortly.Core.ServiceContracts;

public interface IUsersService
{
    Task<AuthenticationResponse?> Login(LoginRequest loginRequest);
    Task<AuthenticationResponse?> Register(RegisterRequest registerRequest);
}