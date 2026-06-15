using FSH.Modules.NamingSeries.Domain;

namespace NamingSeries.Tests.Domain;

public class NamingPatternTests
{
    [Theory]
    [InlineData("FV-{YYYY}-{######}")]
    [InlineData("NC{YY}{MM}{####}")]
    [InlineData("OC-{YYYY}/{####}")]
    [InlineData("{#}")]
    public void Parse_ValidPattern_Succeeds(string pattern)
    {
        var p = NamingPattern.Parse(pattern);
        p.Raw.ShouldBe(pattern);
        p.Tokens.OfType<CounterToken>().Count().ShouldBe(1);
    }

    [Fact]
    public void Parse_PatternWithoutCounter_Throws()
    {
        var ex = Should.Throw<InvalidPatternException>(() => NamingPattern.Parse("FV-{YYYY}"));
        ex.Message.ShouldContain("contador");
    }

    [Fact]
    public void Parse_PatternWithMultipleCounters_Throws()
    {
        var ex = Should.Throw<InvalidPatternException>(() => NamingPattern.Parse("{###}-{###}"));
        ex.Message.ShouldContain("contadores");
    }

    [Fact]
    public void Parse_PatternWithUnknownToken_Throws()
    {
        var ex = Should.Throw<InvalidPatternException>(() => NamingPattern.Parse("FV-{FOO}-{####}"));
        ex.Message.ShouldContain("desconocido");
    }

    [Fact]
    public void Parse_UnclosedBlock_Throws()
    {
        Should.Throw<InvalidPatternException>(() => NamingPattern.Parse("FV-{YYYY-{####}"));
    }

    [Fact]
    public void Format_WithCounterAndYear_PadsCorrectly()
    {
        var p = NamingPattern.Parse("FV-{YYYY}-{######}");
        p.Format(123, new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero))
            .ShouldBe("FV-2026-000123");
    }

    [Fact]
    public void Format_CounterExceedsPadding_KeepsRealLength()
    {
        var p = NamingPattern.Parse("{####}");
        p.Format(12345, DateTimeOffset.UtcNow).ShouldBe("12345");
    }

    [Fact]
    public void Format_ResolvesAllDateTokens_FromGivenInstant()
    {
        var p = NamingPattern.Parse("{YY}{MM}{DD}-{####}");
        var date = new DateTimeOffset(2026, 6, 15, 12, 30, 0, TimeSpan.Zero);
        p.Format(7, date).ShouldBe("260615-0007");
    }
}
