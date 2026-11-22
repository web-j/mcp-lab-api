using McpLab.Mcp.Conventions;

namespace McpLab.ArchitectureTests;

public sealed class PtBrTests
{
    [Theory]
    [InlineData("80", "R$ 80,00")]
    [InlineData("1234.5", "R$ 1.234,50")]
    [InlineData("-20.1", "-R$ 20,10")]
    public void Money_is_always_brazilian_with_plain_spaces(string amount, string expected) =>
        Assert.Equal(expected, PtBr.Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
}
