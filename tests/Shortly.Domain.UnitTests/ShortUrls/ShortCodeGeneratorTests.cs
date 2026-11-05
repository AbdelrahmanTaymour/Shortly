using Shortly.Domain.ShortUrls;

namespace Shortly.Domain.UnitTests.ShortUrls;

public class ShortCodeGeneratorTests
{
    private readonly ShortCodeGenerator _generator = new();

    [Fact]
    public void Generate_Should_ReturnSevenCharacters_ByDefault()
    {
        // Act
        var code = _generator.Generate();

        // Assert
        code.Value.Should().HaveLength(7);
    }

    [Theory]
    [InlineData(ShortCode.MinLength)]
    [InlineData(10)]
    [InlineData(ShortCode.MaxLength)]
    public void Generate_Should_ReturnACodeOfTheRequestedLength(int length)
    {
        // Act
        var code = _generator.Generate(length);

        // Assert
        code.Value.Should().HaveLength(length);
    }

    [Fact]
    public void Generate_Should_OnlyUseAlphanumericCharacters_AndPassShortCodeValidation()
    {
        // Act
        var codes = Enumerable.Range(0, 500).Select(_ => _generator.Generate().Value).ToList();

        // Assert
        codes.Should().OnlyContain(c => c.All(char.IsAsciiLetterOrDigit));
        codes.Should().OnlyContain(c => ShortCode.Create(c).IsSuccess);
    }

    [Fact]
    public void Generate_Should_ProduceDifferentCodes()
    {
        // Act
        var codes = Enumerable.Range(0, 1000).Select(_ => _generator.Generate().Value).ToList();

        // Assert
        codes.Distinct().Should().HaveCount(1000);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(ShortCode.MinLength - 1)]
    [InlineData(ShortCode.MaxLength + 1)]
    [InlineData(int.MaxValue)]
    public void Generate_Should_Throw_When_LengthIsOutsideTheValidRange(int length)
    {
        // Act
        var act = () => _generator.Generate(length);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("length");
    }
}
