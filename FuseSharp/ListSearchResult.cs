using System.Collections.Generic;

namespace FuseSharp
{
    public class ListSearchResult
    {
        public ListSearchResult(int index, double score, IEnumerable<ClosedRange> ranges)
        {
            Index = index;
            Score = score;
            Ranges = ranges;
        }

        public int Index { get; }
        public double Score { get; }
        public IEnumerable<ClosedRange> Ranges { get; }
    }
}
