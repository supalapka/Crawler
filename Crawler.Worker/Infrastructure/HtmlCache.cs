using System.Collections.Concurrent;

namespace Crawler.Worker.Infrastructure
{
    internal class HtmlCache
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();
        public int Count => _cache.Count;

        public void Store(string url, string html) => _cache[url] = html;

        public bool TryGet(string url, out string html) => _cache.TryGetValue(url, out html);

        public IEnumerable<KeyValuePair<string, string>> GetAll() => _cache;

        public bool Any() => _cache.Any();
    }
}
