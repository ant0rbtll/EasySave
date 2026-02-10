namespace EasySave.UI.Tests;

public class CommandLineParserTests
{
    private readonly CommandLineParser _parser = new();

    [Fact]
    public void Parse_WithNullArgs_ThrowsInvalidArgument()
    {
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(null!));
        Assert.Equal("error_parser_arg_null", ex.Message);
    }

    [Fact]
    public void Parse_WithEmptyArgs_ThrowsInvalidArgument()
    {
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse([]));
        Assert.Equal("error_parser_arg_null", ex.Message);
    }

    [Fact]
    public void Parse_WithSingleNumber_ReturnsOneId()
    {
        var ids = _parser.Parse(["5"]);
        Assert.Equal(new[] { 5 }, ids);
    }

    [Fact]
    public void Parse_WithSemicolonList_ReturnsAllIds()
    {
        var ids = _parser.Parse(["1;3;5"]);
        Assert.Equal(new[] { 1, 3, 5 }, ids);
    }

    [Fact]
    public void Parse_WithRange_ReturnsExpandedIds()
    {
        var ids = _parser.Parse(["2-4"]);
        Assert.Equal(new[] { 2, 3, 4 }, ids);
    }

    [Fact]
    public void Parse_WithDescendingRange_ThrowsInvalidArgument()
    {
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(["4-2"]));
        Assert.Equal("error_parser_arg_invalid", ex.Message);
    }

    [Fact]
    public void Parse_WithInvalidRangeFormat_ThrowsInvalidArgument()
    {
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(["1-2-3"]));
        Assert.Equal("error_parser_arg_invalid", ex.Message);
    }

    [Fact]
    public void Parse_WithEmptySemicolonPart_ThrowsInvalidArgument()
    {
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(["1;;3"]));
        Assert.Equal("error_parser_arg_invalid", ex.Message);
    }

    [Fact]
    public void Parse_WithNonNumericPart_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => _parser.Parse(["1;a;3"]));
    }
}
