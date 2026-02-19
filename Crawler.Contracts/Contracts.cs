namespace Crawler.Contracts
{
    public record StartCrawl(string Coin);
    public record UrlMatched(string Coin, string Url);
}
