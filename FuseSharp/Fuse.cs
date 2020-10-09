using System;
using System.Collections.Generic;
using System.Linq;

namespace FuseSharp
{
    public class Fuse
    {
        private readonly int _location;
        private readonly int _distance;
        private readonly double _threshold;
        private readonly int _maxPatternLength;
        private readonly bool _isCaseSensitive;
        private readonly bool _tokenize;

        /// <summary>Creates a new instance of `Fuse`</summary>
        /// <param name="location">Approximately where in the text is the pattern expected to be found. Defaults to `0`</param>
        /// <param name="distance">Determines how close the match must be to the fuzzy `location` (specified above). An exact letter match which is `distance` characters away from the fuzzy location would score as a complete mismatch.
        ///     A distance of `0` requires the match be at the exact `location` specified, a `distance` of `1000` would require a perfect match to be within `800` characters of the fuzzy location to be found using a 0.8 threshold. Defaults to `100`
        /// </param>
        /// <param name="threshold">At what point does the match algorithm give up. A threshold of `0.0` requires a perfect match (of both letters and location), a threshold of `1.0` would match anything. Defaults to `0.6`</param>
        /// <param name="maxPatternLength">The maximum valid pattern length. The longer the pattern, the more intensive the search operation will be. If the pattern exceeds the `maxPatternLength`, the `search` operation will return `nil`.
        ///     Why is this important? [Read this](https://en.wikipedia.org/wiki/Word_(computer_architecture)#Word_size_choice). Defaults to `32`
        /// </param>
        /// <param name="isCaseSensitive">Indicates whether comparisons should be case sensitive. Defaults to `false`</param>
        /// <param name="tokenize">When true, the search algorithm will search individual words **and** the full string, computing the final score as a function of both.
        ///     Note that when `tokenize` is `true`, the `threshold`, `distance`, and `location` are inconsequential for individual tokens.
        /// </param>
        public Fuse(int location = 0, int distance = 100, double threshold = 0.6, int maxPatternLength = 32, bool isCaseSensitive = false, bool tokenize = false)
        {
            _location = location;
            _distance = distance;
            _threshold = threshold;
            _maxPatternLength = maxPatternLength;
            _isCaseSensitive = isCaseSensitive;
            _tokenize = tokenize;
        }

        /// <summary>
        /// Creates a pattern tuple.
        /// </summary>
        /// <param name="aString">A string from which to create the pattern tuple</param>
        /// <returns>
        /// A tuple containing pattern metadata
        /// </returns>
        public Pattern? CreatePattern(string aString)
        {
            var pattern = _isCaseSensitive ? aString : aString.ToLower();
            var len = pattern.Length;

            if (len == 0)
            {
                return null;
            }

            return new Pattern(pattern, len, 1 << (len - 1), FuseUtilities.CalculatePatternAlphabet(pattern));
        }

        /// <summary>Searches for a pattern in a given string.</summary>
        /// <param name="pattern">The pattern to search for. This is created by calling `createPattern`</param>
        /// <param name="aString">The string in which to search for the pattern</param>
        /// <returns>A tuple containing a `score` between `0.0` (exact match) and `1` (not a match), and `ranges` of the matched characters. If no match is found will return null.</returns>
        /// <example>
        ///     var fuse = new Fuse();
        ///     var pattern = fuse.CreatePattern("some text");
        ///     fuse.search(pattern, "some string");
        /// </example>
        public SearchResult? Search(Pattern? pattern, string aString)
        {
            if (pattern == null)
            {
                return null;
            }

            //If tokenize is set we will split the pattern into individual words and take the average which should result in more accurate matches
            if (_tokenize)
            {
                //Split this pattern by the space character
                var wordPatterns = pattern.Text.Split(' ').Select(CreatePattern);

                //Get the result for testing the full pattern string. If 2 strings have equal individual word matches this will boost the full string that matches best overall to the top
                var fullPatternResult = SearchInternal(pattern, aString);

                //Reduce all the word pattern matches and the full pattern match into a totals tuple
                var results = wordPatterns.Aggregate(fullPatternResult, (totalResult, pattern) =>
                {
                    var result = SearchInternal(pattern, aString);
                    return new SearchResult(totalResult.Score + result.Score, totalResult.Ranges.Concat(result.Ranges));
                });

                //Average the total score by dividing the summed scores by the number of word searches + the full string search. Also remove any range duplicates since we are searching full string and words individually.
                var score = results.Score / (double)(wordPatterns.Count() + 1);
                var ranges = results.Ranges;

                //If the averaged score is 1 then there were no matches so return nil. Otherwise return the average result
                return score == 1 ? null : new SearchResult(score, ranges);
            }
            else
            {
                var result = SearchInternal(pattern, aString);

                //If the averaged score is 1 then there were no matches so return nil. Otherwise return the average result
                return result.Score == 1 ? null : result;
            }
        }

        /// <summary>Searches for a pattern in a given string.</summary>
        /// <param name="pattern">The pattern to search for. This is created by calling `CreatePattern`</param>
        /// <param name="aString">The string in which to search for the pattern</param>
        /// <returns>A tuple containing a `score` between `0.0` (exact match) and `1` (not a match), and `ranges` of the matched characters. If no match is found will return a tuple with score of 1 and empty array of ranges.</returns>
        private SearchResult SearchInternal(Pattern pattern, string aString)
        {
            var text = aString;

            if (!_isCaseSensitive)
            {
                text = text.ToLower();
            }

            var textLength = text.Length;

            // Exact match
            if (pattern.Text == text)
            {
                return new SearchResult(0, new[] { new ClosedRange(0, textLength - 1) });
            }

            var location = _location;
            var distance = _distance;
            var threshold = _threshold;

            int getBestLocation()
            {
                var index = text.IndexOf(pattern.Text, location);
                if (index != -1)
                {
                    return index;
                }
                return 0;
            }
            var bestLocation = getBestLocation();

            // A mask of the matches. We'll use to determine all the ranges of the matches
            var matchMaskArr = Enumerable.Repeat(0, textLength).ToArray();

            // Get all exact matches, here for speed up
            var index = text.IndexOf(pattern.Text, bestLocation);
            while (index != -1)
            {
                var i = index;
                var _score = FuseUtilities.CalculateScore(pattern.Len, 0, i, location, distance);

                threshold = Math.Min(threshold, _score);
                bestLocation = i + pattern.Len;
                index = text.IndexOf(pattern.Text, bestLocation);

                var idx = 0;
                while (idx < pattern.Len)
                {
                    matchMaskArr[i + idx] = 1;
                    idx += 1;
                }
            }

            // Reset the best location
            bestLocation = 0;

            var score = 1.0;
            var binMax = pattern.Len + textLength;
            var lastBitArr = new int[0];

            var textCount = text.Length;

            // Magic begins now
            for (int i = 0; i < pattern.Len; i++)
            {
                // Scan for the best match; each iteration allows for one more error.
                // Run a binary search to determine how far from the match location we can stray at this error level.
                var binMin = 0;
                var binMid = binMax;

                while (binMin < binMid)
                {
                    if (FuseUtilities.CalculateScore(pattern.Len, i, location, location + binMid, distance) <= threshold)
                    {
                        binMin = binMid;
                    }
                    else
                    {
                        binMax = binMid;
                    }
                    binMid = ((binMax - binMin) / 2) + binMin;
                }

                // Use the result from this iteration as the maximum for the next.
                binMax = binMid;
                var start = Math.Max(1, location - binMid + 1);
                var finish = Math.Min(location + binMid, textLength) + pattern.Len;

                // Initialize the bit array
                var bitArr = Enumerable.Repeat(0, finish + 2).ToArray();
                bitArr[finish + 1] = (1 << i) - 1;

                if (start > finish)
                {
                    continue;
                }

                int? currentLocationIndex = null;

                for (int j = finish; j >= start; j--)
                {
                    var currentLocation = j - 1;

                    // Need to check for `nil` case, since `patternAlphabet` is a sparse hash
                    int getCharMatch()
                    {
                        if (currentLocation < textCount)
                        {
                            currentLocationIndex = currentLocationIndex != null ? Math.Max(0, currentLocationIndex.Value - 1) : currentLocation; // todo not too sure here..
                            var @char = text.ElementAt(currentLocationIndex.Value);

                            if (pattern.Alphabet.ContainsKey(@char))
                            {
                                return pattern.Alphabet[@char];
                            }
                        }
                        return 0;
                    }
                    var charMatch = getCharMatch();

                    // A match is found
                    if (charMatch != 0)
                    {
                        matchMaskArr[currentLocation] = 1;
                    }

                    // First pass: exact match
                    bitArr[j] = ((bitArr[j + 1] << 1) | 1) & charMatch;

                    // Subsequent passes: fuzzy match
                    if (i > 0)
                    {
                        bitArr[j] |= (((lastBitArr[j + 1] | lastBitArr[j]) << 1) | 1) | lastBitArr[j + 1];
                    }

                    if ((bitArr[j] & pattern.Mask) != 0)
                    {
                        score = FuseUtilities.CalculateScore(pattern.Len, i, location, currentLocation, distance);

                        // This match will almost certainly be better than any existing match. But check anyway.
                        if (score <= threshold)
                        {
                            // Indeed it is
                            threshold = score;
                            bestLocation = currentLocation;

                            if (bestLocation > location)
                            {
                                // When passing `bestLocation`, don't exceed our current distance from the expected `location`.
                                start = Math.Max(1, 2 * location - bestLocation);
                            }
                            else
                            {
                                // Already passed `location`. No point in continuing.
                                break;
                            }
                        }
                    }
                }

                // No hope for a better match at greater error levels
                if (FuseUtilities.CalculateScore(pattern.Len, i + 1, location, location, distance) > threshold)
                {
                    break;
                }

                lastBitArr = bitArr;
            }

            return new SearchResult(score, FuseUtilities.FindRanges(matchMaskArr));
        }

        /// Searches for a text pattern in a given string.
        ///
        ///     let fuse = Fuse()
        ///     fuse.search("some text", in: "some string")
        ///
        /// **Note**: if the same text needs to be searched across many strings, consider creating the pattern once via `createPattern`, and then use the other `search` function. This will improve performance, as the pattern object would only be created once, and re-used across every search call:
        ///
        ///     let fuse = Fuse()
        ///     let pattern = fuse.createPattern(from: "some text")
        ///     fuse.search(pattern, in: "some string")
        ///     fuse.search(pattern, in: "another string")
        ///     fuse.search(pattern, in: "yet another string")
        ///
        /// - Parameters:
        ///   - text: the text string to search for.
        ///   - aString: The string in which to search for the pattern
        /// - Returns: A tuple containing a `score` between `0.0` (exact match) and `1` (not a match), and `ranges` of the matched characters.
        public SearchResult? Search(string text, string aString)
        {
            return Search(CreatePattern(text), aString);
        }

        /// Searches for a text pattern in an array of srings
        ///
        /// - Parameters:
        ///   - text: The pattern string to search for
        ///   - aList: The list of string in which to search
        /// - Returns: A tuple containing the `item` in which the match is found, the `score`, and the `ranges` of the matched characters
        public IEnumerable<ListSearchResult> Search(string text, IEnumerable<string> aList)
        {
            var pattern = CreatePattern(text);

            var items = new List<ListSearchResult>();

            for (int i = 0; i < aList.Count(); i++)
            {
                var item = aList.ElementAt(i);
                var result = Search(pattern, item);
                if (result != null)
                {
                    items.Add(new ListSearchResult(i, result.Score, result.Ranges));
                }
            }

            return items.OrderBy(r => r.Score);
        }

        /// Searches for a text pattern in an array of `Fuseable` objects.
        ///
        /// Each `FuseSearchable` object contains a `properties` accessor which returns `FuseProperty` array. Each `FuseProperty` is a tuple containing a `key` (the value of the property which should be included in the search), and a `weight` (how much "weight" to assign to the score)
        ///
        /// ## Example
        ///
        /// Ensure the object conforms to `Fuseable`:
        ///
        ///     struct Book: Fuseable {
        ///         let title: String
        ///         let author: String
        ///
        ///         var properties: [FuseProperty] {
        ///             return [
        ///                 FuseProperty(name: title, weight: 0.3),
        ///                 FuseProperty(name: author, weight: 0.7),
        ///             ]
        ///         }
        ///     }
        ///
        /// Searching:
        ///
        ///     let books: [Book] = [
        ///         Book(author: "John X", title: "Old Man's War fiction"),
        ///         Book(author: "P.D. Mans", title: "Right Ho Jeeves")
        ///     ]
        ///
        ///     let fuse = Fuse()
        ///     let results = fuse.search("Man", in: books)
        ///
        /// - Parameters:
        ///   - text: The pattern string to search for
        ///   - aList: The list of `Fuseable` objects in which to search
        /// - Returns: A list of `CollectionResult` objects
        public IEnumerable<FusableListSearchResult> Search(string text, IEnumerable<IFuseable> aList)
        {
            var pattern = CreatePattern(text);

            var collectionResult = new List<FusableListSearchResult>();

            for (int i = 0; i < aList.Count(); i++)
            {
                var item = aList.ElementAt(i);

                var scores = new List<double>();
                var totalScore = 0.0;

                var propertyResults = new List<FusableSearchPropertyResult>();

                foreach (var property in item.Properties)
                {
                    var value = property.Name;

                    var result = Search(pattern, value);
                    if (result != null)
                    {
                        var weight = property.Weight == 1 ? 1 : 1 - property.Weight;
                        var score = (result.Score == 0 && weight == 1 ? 0.001 : result.Score) * weight;
                        totalScore += score;

                        scores.Add(score);

                        propertyResults.Add(new FusableSearchPropertyResult(property.Name, score, result.Ranges));
                    }
                }

                if (scores.Count == 0)
                {
                    continue;
                }

                collectionResult.Add(new FusableListSearchResult(i, totalScore / (double)scores.Count, propertyResults));
            }

            return collectionResult.OrderBy(r => r.Score);
        }
    }
}
