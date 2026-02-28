using Crawler.Worker.Parsing;

namespace Crawler.Worker.Tests;

public class ForkLogFilterParsingTests
{
    private readonly ForkLogFilterParsing _sut = new();

    private static string Wrap(string bodyHtml) =>
        $"<html><body><div class=\"post_content\">{bodyHtml}</div></body></html>";

    [Fact]
    public async Task ContentMatchFilter_ReturnsTrue_WhenFilterWordPresent()
    {
        var html = Wrap("<p>Bitcoin is a cryptocurrency.</p>");
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.True(result);
    }

    [Fact]
    public async Task ContentMatchFilter_IsCaseInsensitive()
    {
        var html = Wrap("<p>bitcoin is a cryptocurrency.</p>");
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.True(result);
    }

    [Fact]
    public async Task ContentMatchFilter_ReturnsFalse_WhenFilterWordAbsent()
    {
        var html = Wrap("<p>Ethereum is a platform.</p>");
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.False(result);
    }

    [Fact]
    public async Task ContentMatchFilter_ReturnsFalse_WhenFilterIsSubstringOfWord()
    {
        var html = Wrap("<p>BitcoinCash is a fork.</p>");
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.False(result);
    }

    [Fact]
    public async Task ContentMatchFilter_ReturnsFalse_WhenNoPostContentDiv()
    {
        var html = "<html><body><div class=\"other\"><p>Bitcoin</p></div></body></html>";
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.False(result);
    }

    [Fact]
    public async Task ContentMatchFilter_ReturnsFalse_WhenPostContentIsEmpty()
    {
        var html = Wrap("");
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.False(result);
    }

    [Fact]
    public async Task ContentMatchFilter_MatchesFilterAtStartOfText()
    {
        var html = Wrap("<p>Bitcoin dominates the market.</p>");
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.True(result);
    }

    [Fact]
    public async Task ContentMatchFilter_MatchesFilterAtEndOfText()
    {
        var html = Wrap("<p>The market leader is Bitcoin</p>");
        var result = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.True(result);
    }

    [Fact]
    public async Task ContentMatchFilter_ReusesRegexAcrossMultipleCalls()
    {
        var html = Wrap("<p>Bitcoin is rising.</p>");
        var result1 = await _sut.ContentMatchFilter(html, "Bitcoin");
        var result2 = await _sut.ContentMatchFilter(html, "Bitcoin");
        Assert.True(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task ContentMatchFilter_ThrowsArgumentException_WhenFilterIsEmpty()
    {
        var html = Wrap("<p>Some content.</p>");
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ContentMatchFilter(html, ""));
    }

    [Fact]
    public async Task ContentMatchFilter_ThrowsArgumentException_WhenFilterIsWhitespace()
    {
        var html = Wrap("<p>Some content.</p>");
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ContentMatchFilter(html, "   "));
    }
}
