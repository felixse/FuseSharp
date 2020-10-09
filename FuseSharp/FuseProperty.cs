namespace FuseSharp
{
    public class FuseProperty
    {
        public FuseProperty(string name)
            : this(name, 1.0)
        {
        }

        public FuseProperty(string name, double weight)
        {
            Name = name;
            Weight = weight;
        }

        public string Name { get; }
        public double Weight { get; }
    }
}
