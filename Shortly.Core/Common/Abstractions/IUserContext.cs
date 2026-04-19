namespace Shortly.Core.Common.Abstractions;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    Guid CurrentUserId { get; }
    string Username { get; }
    string CurrentUserEmail { get; }
    long Permissions { get; }
    string Jti { get; }
}