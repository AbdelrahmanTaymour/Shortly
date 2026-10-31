namespace Shortly.Core.Abstractions.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}