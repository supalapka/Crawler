namespace Crawler.Worker.Infrastructure
{
    public class VisitedSet
    {
        private readonly HashSet<string> _visited = new();

        public bool TryAdd(string url) => _visited.Add(url);
    }
}
