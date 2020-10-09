using System.Collections.Generic;

namespace FuseSharp
{
    public class Pattern
    {
        public Pattern(string text, int len, int mask, IDictionary<char, int> alphabet)
        {
            Text = text;
            Len = len;
            Mask = mask;
            Alphabet = alphabet;
        }

        public string Text { get; }
        public int Len { get; }
        public int Mask { get; }
        public IDictionary<char, int> Alphabet { get; }
    }
}
