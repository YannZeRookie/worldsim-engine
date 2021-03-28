using System;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Engine
{
    public abstract class JM2 : IJM2
    {
        public string Id { get; set; }
        public float? Efficiency { get; set; }

        public JM2()
        {
            Efficiency = null;
        }

        public virtual void Restart()
        {
            Efficiency = null;
        }

        public virtual void Step(IDictionary<string, float> stocks, Time currentTime,
            IDictionary<string, Allocation> allocations, IDictionary<string, float> output)
        {
        }

        public virtual string GetExtraLine(int extraLine)
        {
            return "";
        }

        public string GetExtraLine0()
        {
            return "JM2:";
        }

        public virtual int NbExtraLines()
        {
            return 0;
        }

        public virtual int ExtraWidth()
        {
            return 0;
        }

        public virtual void DescribeDemand(Time currentTime, IDictionary<string, float> demand)
        {
        }
    }
}
