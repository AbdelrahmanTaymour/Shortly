namespace Shortly.Core.Abstractions.Authentication;

public interface ITokenHasher
{
    string GenerateToken();
    string Hash(string token);
}