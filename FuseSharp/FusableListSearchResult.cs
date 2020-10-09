using System.Collections.Generic;

namespace FuseSharp
{
    public class FusableListSearchResult
    {
        public FusableListSearchResult(int index, double score, IEnumerable<FusableSearchPropertyResult> results)
        {
            Index = index;
            Score = score;
            Results = results;
        }

        public int Index { get; }
        public double Score { get; }
        public IEnumerable<FusableSearchPropertyResult> Results { get; }
    }
}
