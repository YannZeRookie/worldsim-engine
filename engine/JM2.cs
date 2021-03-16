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

        public virtual void Step(Map map, IDictionary<string, float> stocks, Time currentTime, float annualDivider,
            IDictionary<string, float> output)
        {
        }

        /// <summary>
        /// Generic method to look for resource to consume
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="needs"></param>
        /// <param name="map"></param>
        /// <param name="stocks"></param>
        /// <returns></returns>
        public float ConsumeResource(string resourceId, float needs, Map map, IDictionary<string, float> stocks)
        {
            //-- Try local stock first
            float found = Math.Min(stocks[resourceId], needs);
            stocks[resourceId] -= found;
            float notFound = needs - found;
            if (notFound > 0)
            {
                float foundElsewhere = map.FindResource(resourceId, notFound);
                found += foundElsewhere;
            }

            return found;
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
    }
}
