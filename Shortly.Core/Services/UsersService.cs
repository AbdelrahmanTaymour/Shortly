using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;

namespace Shortly.Core.Services;

public class UsersService(IUserRepository userRepository, IConfiguration configuration): IUsersService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthenticationResponse?> Login(LoginRequest loginRequest)
    {
        var user = await _userRepository.GetUserByEmail(loginRequest.Email);
    
        if (user == null)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        // Verify the password matches the hashed password
        if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
        {
            throw new AuthenticationException("Invalid email or password.");
        }
        
        // Generate JWT Token
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var token = GenerateToken(authClaims);
        
        return new AuthenticationResponse(user.Id, user.Name, user.Email, new JwtSecurityTokenHandler().WriteToken(token),
            token.ValidTo, true);
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
        
        user = await _userRepository.AddUser(user);
        
        // Generate JWT Token
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };
        
        var token = GenerateToken(authClaims);

        return new AuthenticationResponse(user.Id, user.Name, user.Email, new JwtSecurityTokenHandler().WriteToken(token), 
            token.ValidTo, true);
    }

    private JwtSecurityToken GenerateToken(IEnumerable<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

         return new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.Now.AddHours(3), // Token expiration
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
    }
    
}