using System.Collections.Concurrent;

namespace HandshakesByDC_BEAssignment.Models
{
    public class SampleData
    {
        public ConcurrentBag<string> Logs { get; set; } = new();
        public List<string[]> CsvData { get; set; } = new();
    }
}
