namespace Shortly.Domain.ShortUrls;

/// <summary>Defines a contract for generating random short codes to enable testability and collision simulation.</summary>
public interface IShortCodeGenerator
{
    ShortCode Generate(int length = 7);
}