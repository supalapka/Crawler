namespace Crawler.Worker.Infrastructure
{
    public class VisitedSet
    {
        private readonly HashSet<string> _visited = new();
        public int Count => _visited.Count;

        public bool TryAdd(string url) => _visited.Add(url);
    }
}
