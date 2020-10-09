using System;
using System.Collections.Generic;
using System.Linq;

namespace FuseSharp
{
    public static class FuseUtilities
    {
        /// Computes the score for a match with `e` errors and `x` location.
        ///
        /// - Parameter pattern: Pattern being sought.
        /// - Parameter e: Number of errors in match.
        /// - Parameter x: Location of match.
        /// - Parameter loc: Expected location of match.
        /// - Parameter scoreTextLength: Coerced version of text's length.
        /// - Returns: Overall score for match (0.0 = good, 1.0 = bad).
        public static double CalculateScore(string pattern, int e, int x, int loc, int distance)
            => CalculateScore(pattern.Length, e, x, loc, distance);

        /// Computes the score for a match with `e` errors and `x` location.
        ///
        /// - Parameter patternLength: Length of pattern being sought.
        /// - Parameter e: Number of errors in match.
        /// - Parameter x: Location of match.
        /// - Parameter loc: Expected location of match.
        /// - Parameter scoreTextLength: Coerced version of text's length.
        /// - Returns: Overall score for match (0.0 = good, 1.0 = bad).
        public static double CalculateScore(int patternLength, int e, int x, int loc, int distance)
        {
            var accuracy = (double)e / (double)patternLength;
            var proximity = Math.Abs(x - loc);
            if (distance == 0)
            {
                return proximity != 0 ? 1.0 : accuracy;
            }

            return accuracy + ((double)proximity / (double)distance);
        }

        /// Initializes the alphabet for the Bitap algorithm
        ///
        /// - Parameter pattern: The text to encode.
        /// - Returns: Hash of character locations.
        public static Dictionary<char, int> CalculatePatternAlphabet(string pattern)
        {
            var len = pattern.Length;
            var mask = new Dictionary<char, int>();

            for (int i = 0; i < len; i++)
            {
                var c = pattern[i];
                if (!mask.ContainsKey(c))
                {
                    mask[c] = 0;
                }
                mask[c] |= (1 << (len - i - 1));
            }

            return mask;
        }

        /// Returns an array of `CountableClosedRange<Int>`, where each range represents a consecutive list of `1`s.
        ///
        ///     let arr = [0, 1, 1, 0, 1, 1, 1 ]
        ///     let ranges = findRanges(arr)
        ///     // [{startIndex 1, endIndex 2}, {startIndex 4, endIndex 6}
        ///
        /// - Parameter mask: A string representing the value to search for.
        ///
        /// - Returns: `CountableClosedRange<Int>` array.
        public static IEnumerable<ClosedRange> FindRanges(IEnumerable<int> mask)
        {
            var ranges = new List<ClosedRange>();
            var start = -1;

            for (int n = 0; n < mask.Count(); n++)
            {
                var bit = mask.ElementAt(n);
                if (start == -1 && bit == 1)
                {
                    start = n;
                }
                else if (start != -1 && bit == 0)
                {
                    ranges.Add(new ClosedRange(start, n - 1));
                    start = -1;
                }
            }

            if (mask.Last() == 1)
            {
                ranges.Add(new ClosedRange(start, mask.Count() - 1));
            }

            return ranges;
        }
    }
}
