using Shortly.Domain.Users;

namespace Shortly.API.Controllers.Admin;

public sealed record AdminCreateUserRequest(string Email, string Username, string Password, UserRole Role);