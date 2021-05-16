using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Model
{
    public abstract class JM2 : IJM2
    {
        public JM2(IDictionary<string, object> init)
        {
            _init = init;
            Efficiency = null;
        }

        public string Id { get; set; }
        protected IDictionary<string, object> _init;
        public IDictionary<string, object> Init { get => _init; }
        public float? Efficiency { get; set; }

        public virtual string GetExtraLine(int extraLine)
        {
            return "";
        }

        public virtual int NbExtraLines()
        {
            return 0;
        }

        public virtual int ExtraWidth()
        {
            return 0;
        }

        public virtual void Restart()
        {
            Efficiency = null;
        }

        public virtual void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
        }

        public string GetExtraLine0()
        {
            return "JM2:";
        }

        public virtual void DescribeDemand(Time currentTime, IDictionary<string, float> demand)
        {
        }
    }
}
