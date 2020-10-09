using System.Collections.Generic;

namespace FuseSharp
{
    public class FusableSearchPropertyResult
    {
        public FusableSearchPropertyResult(string key, double score, IEnumerable<ClosedRange> ranges)
        {
            Key = key;
            Score = score;
            Ranges = ranges;
        }

        public string Key { get; }
        public double Score { get; }
        public IEnumerable<ClosedRange> Ranges { get; }
    }
}
