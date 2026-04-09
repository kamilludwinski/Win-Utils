using WinUtil.Services;
using Xunit;

namespace WinUtil.Tests;

public sealed class ExclusionCsvParserTests
{
    [Theory]
    [InlineData("a,b", new[] { "a", "b" })]
    [InlineData("foo,", new[] { "foo", "" })]
    [InlineData(",bar", new[] { "", "bar" })]
    public void ParseLine_splits_unquoted_fields(string line, string[] expected)
    {
        var fields = ExclusionCsvParser.ParseLine(line);
        Assert.Equal(expected, fields);
    }

    [Fact]
    public void ParseLine_quoted_field_with_comma()
    {
        var fields = ExclusionCsvParser.ParseLine("\"a,b\",c");
        Assert.Equal(new[] { "a,b", "c" }, fields);
    }

    [Fact]
    public void ParseLine_doubled_quote_inside_quotes()
    {
        var fields = ExclusionCsvParser.ParseLine("\"say \"\"hi\"\"\",x");
        Assert.Equal(new[] { "say \"hi\"", "x" }, fields);
    }

    [Fact]
    public void LoadNames_skips_header_and_comments()
    {
        var lines = new[]
        {
            "# comment",
            "",
            "name,description",
            "notepad,Notes",
            "  msedge  , browser ",
        };
        var names = ExclusionCsvParser.LoadNames(lines);
        Assert.Equal(new[] { "notepad", "msedge" }, names);
    }

    [Fact]
    public void LoadExclusionEntries_preserves_descriptions()
    {
        var lines = new[]
        {
            "name,description",
            "foo,bar baz",
            "\"x,y\",\"desc, with comma\"",
        };
        var rows = ExclusionCsvParser.LoadExclusionEntries(lines);
        Assert.Equal(2, rows.Count);
        Assert.Equal("foo", rows[0].Name);
        Assert.Equal("bar baz", rows[0].Description);
        Assert.Equal("x,y", rows[1].Name);
        Assert.Equal("desc, with comma", rows[1].Description);
    }

    [Fact]
    public void LoadLegacyCommentedLines_skips_hash_and_blank()
    {
        var lines = new[] { "# x", "", "proc1", "  proc2  " };
        var names = ExclusionCsvParser.LoadLegacyCommentedLines(lines);
        Assert.Equal(new[] { "proc1", "proc2" }, names);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"x\"", "\"say \"\"x\"\"\"")]
    public void EscapeField_quotes_when_needed(string input, string expected)
    {
        Assert.Equal(expected, ExclusionCsvParser.EscapeField(input));
    }

    [Fact]
    public void FormatRow_joins_columns()
    {
        Assert.Equal("a,b", ExclusionCsvParser.FormatRow("a", "b"));
        Assert.Equal("\"a,b\",c", ExclusionCsvParser.FormatRow("a,b", "c"));
    }

    [Fact]
    public void Utf8BomHeader_starts_with_utf8_bom()
    {
        var bytes = ExclusionCsvParser.Utf8BomHeader("name,description");
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }
}
