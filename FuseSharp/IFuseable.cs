using System.Collections.Generic;

namespace FuseSharp
{
    public interface IFuseable
    {
        IEnumerable<FuseProperty> Properties { get; }
    }
}
