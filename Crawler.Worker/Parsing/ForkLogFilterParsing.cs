using AngleSharp;
using AngleSharp.Dom;
using System.Text.RegularExpressions;

namespace Crawler.Worker.Parsing
{
    internal class ForkLogFilterParsing
    {
        private readonly IBrowsingContext _context;
        private readonly Dictionary<string, Regex> _regexCache = new(StringComparer.OrdinalIgnoreCase);

        public ForkLogFilterParsing()
        {
            _context = BrowsingContext.New(Configuration.Default);
        }

        public async Task<bool> ContentMatchFilter(string html, string filter)
        {
            var doc = await _context.OpenAsync(req => req.Content(html));

            var contentText = GetForkLogContent(doc);
            if (contentText is null)
                return false;

            var regex = GetOrBuildRegex(filter);

            return regex.IsMatch(contentText);
        }

        private string GetForkLogContent(IDocument articleDoc)
        {
            var contentElement = articleDoc.QuerySelector("div.post_content");
            if (contentElement == null)
                return string.Empty;

            contentElement.QuerySelector(".aside_subscribe_blk")?.Remove();
            return contentElement.TextContent ?? string.Empty;
        }

        private Regex GetOrBuildRegex(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                throw new ArgumentException("Filter cannot be empty", nameof(filter));

            if (!_regexCache.TryGetValue(filter, out var regex))
            {
                regex = new Regex(
                    $@"(?<!\p{{L}}){Regex.Escape(filter)}(?!\p{{L}})",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
                );
                _regexCache[filter] = regex;
            }

            return regex;
        }
    }
}
