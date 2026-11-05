using System.Reflection;
using Shortly.Domain.Abstractions;

namespace Shortly.Domain.UnitTests;

/// <summary>Guards the whole error catalog: clients and logs rely on every error code being unique, stable and descriptive.</summary>
public class ErrorCatalogTests
{
    private sealed record CatalogEntry(string Owner, string Field, Error Error);

    private static readonly IReadOnlyList<CatalogEntry> Catalog = typeof(Error).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true } && t.Name.EndsWith("Errors", StringComparison.Ordinal))
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Error))
            .Select(f => new CatalogEntry(t.Name, f.Name, (Error)f.GetValue(null)!)))
        .ToList();

    [Fact]
    public void Catalog_Should_BeDiscovered()
    {
        // Assert: guards the reflection above against silently scanning nothing
        Catalog.Select(e => e.Owner).Distinct().Should().HaveCountGreaterThanOrEqualTo(7);
        Catalog.Should().HaveCountGreaterThan(50);
    }

    [Fact]
    public void Every_Error_Should_HaveACodeAndAMessage()
    {
        // Assert
        Catalog.Where(e => string.IsNullOrWhiteSpace(e.Error.Code) || string.IsNullOrWhiteSpace(e.Error.Message))
            .Select(e => $"{e.Owner}.{e.Field}")
            .Should().BeEmpty();
    }

    [Fact]
    public void Every_Error_Should_HaveAUniqueCode()
    {
        // Assert
        Catalog.GroupBy(e => e.Error.Code)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(e => $"{e.Owner}.{e.Field}"))}")
            .Should().BeEmpty();
    }

    [Fact]
    public void Every_Error_Should_UseTheCodeFormat_Prefix_Dot_Name()
    {
        // Assert
        Catalog.Where(e => !System.Text.RegularExpressions.Regex.IsMatch(e.Error.Code, @"^[A-Z][A-Za-z]+\.[A-Z][A-Za-z]+$"))
            .Select(e => $"{e.Owner}.{e.Field} = {e.Error.Code}")
            .Should().BeEmpty();
    }

    [Fact]
    public void Every_Error_Should_BePrefixedWithItsFeatureName()
    {
        // Assert: ShortUrlErrors -> "ShortUrl.*", UserErrors -> "User.*", ...
        Catalog.Where(e => !e.Error.Code.StartsWith(e.Owner[..^"Errors".Length] + ".", StringComparison.Ordinal))
            .Select(e => $"{e.Owner}.{e.Field} = {e.Error.Code}")
            .Should().BeEmpty();
    }

    [Fact]
    public void Every_Error_Should_UseItsFieldNameAsTheCodeSuffix()
    {
        // Assert
        Catalog.Where(e => !e.Error.Code.EndsWith("." + e.Field, StringComparison.Ordinal))
            .Select(e => $"{e.Owner}.{e.Field} = {e.Error.Code}")
            .Should().BeEmpty();
    }

    [Fact]
    public void Every_Error_Should_EndItsMessageWithAFullStop()
    {
        // Assert
        Catalog.Where(e => !e.Error.Message.TrimEnd().EndsWith('.'))
            .Select(e => $"{e.Owner}.{e.Field} = \"{e.Error.Message}\"")
            .Should().BeEmpty();
    }

    [Fact]
    public void Every_Error_Should_BeAKnownNonDefaultType_OrDeliberatelyAFailure()
    {
        // Assert: no error is accidentally left as the generic Failure when it describes a client mistake
        Catalog.Where(e => e.Error.Type == ErrorType.Failure)
            .Select(e => $"{e.Owner}.{e.Field}")
            .Should().BeEquivalentTo(["ShortUrlErrors.InvalidOwnerType"], "only configuration-style problems stay generic failures");
    }
}
