using System.Collections.Generic;

namespace WorldSim.Engine
{
    public class JM2Mine : JM2Source
    {
        public JM2Mine(IDictionary<string, object> init) : base(init)
        {
            Id = "mine";
        }
    }
}