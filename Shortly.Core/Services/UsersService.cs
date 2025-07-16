using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
//using Shortly.Core.Authentication;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;

namespace Shortly.Core.Services;

public class UsersService(IUserRepository userRepository, IConfiguration configuration, IJwtService jwtService): IUsersService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly IJwtService _jwtService = jwtService;

    public async Task<AuthenticationResponse?> Login(LoginRequest loginRequest)
    {
        var user = await _userRepository.GetUserByEmail(loginRequest.Email);
    
        if (user == null)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        //Verify the password matches the hashed password
        if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        var tokens = _jwtService.GenerateTokensAsync(user);
        
        return new AuthenticationResponse(user.Id, user.Name, user.Email, tokens.Result, true);
    }

    public async Task<AuthenticationResponse?> Register(RegisterRequest registerRequest)
    {
        bool userExists =
            await _userRepository.IsEmailOrUsernameTaken(registerRequest.Email, registerRequest.Username);
        
        if (userExists)
            throw new Exception("User with the same username or email already exists.");
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = registerRequest.Name,
            Email = registerRequest.Email,
            Username = registerRequest.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password), // Hash the Password
        };
        
        var stopwatch2 = Stopwatch.StartNew();
        user = await _userRepository.AddUser(user);
        var tokens = _jwtService.GenerateTokensAsync(user);
        stopwatch2.Stop();
        Console.WriteLine($"With Add and generate tokens in register Time: {stopwatch2.ElapsedMilliseconds}ms");
        
        return new AuthenticationResponse(user.Id, user.Name, user.Email, tokens.Result, true);
    }
}