using Shortly.Domain.Users;

namespace Shortly.Api.Controllers.Admin;

public sealed record AdminCreateUserRequest(string Email, string Username, string Password, UserRole Role);