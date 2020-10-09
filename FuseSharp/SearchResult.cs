using System.Collections.Generic;

namespace FuseSharp
{
    public class SearchResult
    {
        public SearchResult(double score, IEnumerable<ClosedRange> ranges)
        {
            Score = score;
            Ranges = ranges;
        }

        public double Score { get; }
        public IEnumerable<ClosedRange> Ranges { get; }
    }
}
