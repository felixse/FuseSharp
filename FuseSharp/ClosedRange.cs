namespace FuseSharp
{
    public class ClosedRange
    {
        public ClosedRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }
        public int End { get; }
    }
}
