using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WorldSim.Engine
{
    public class JM2Factory : JM2
    {
        private Dictionary<string, float> _opex;
        private Dictionary<string, float> _output;
        private IDictionary<string, object> _init;

        public JM2Factory(IDictionary<string, object> init) : base()
        {
            Id = "factory";
            _init = init;
            _opex = new Dictionary<string, float>();
            _output = new Dictionary<string, float>();
            Restart();
        }

        public override void Restart()
        {
            _opex.Clear();
            foreach (var ol in (IEnumerable) _init["opex"])
            {
                //ok with string: IDictionary<object, object> ol0 = (IDictionary<object, object>) ol;
                //ok with file: IDictionary<string, JToken> ol0 = (IDictionary<string, JToken>) ol;
                // Frankly the following code sucks! Where is my nice OOD gone? All this because the 
                // YAML reader seems to use different classes wherever I read from a file or a string
                if (ol is IDictionary<string, JToken> olj)
                {
                    string resourceId = (string) olj["resource_id"];
                    _opex[resourceId] = Convert.ToSingle(olj["consumption"]);
                }
                else if (ol is IDictionary<object, object> olo)
                {
                    string resourceId = (string) olo["resource_id"];
                    _opex[resourceId] = Convert.ToSingle(olo["consumption"]);
                }
            }

            _output.Clear();
            foreach (var ol in (IEnumerable) _init["output"])
            {
                if (ol is IDictionary<string, JToken> olj)
                {
                    string resourceId = (string) olj["resource_id"];
                    _output[resourceId] = Convert.ToSingle(olj["production"]);
                }
                else if (ol is IDictionary<object, object> olo)
                {
                    string resourceId = (string) olo["resource_id"];
                    _output[resourceId] = Convert.ToSingle(olo["production"]);
                }
            }

            base.Restart();
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            float annualDivider = currentTime.GetAnnualDivider(); 
            //-- Compute the expected efficiency
            float efficiency = 1.0f;
            foreach (var supply in _opex)
            {
                // Check what was allocated to us and compare to our needs
                if (efficiency > 0.0f)
                {
                    float needs = supply.Value / annualDivider;
                    if (needs > 0.0f)
                        efficiency =
                            Math.Min(efficiency,
                                allocator.GetAllocation(supply.Key, cell) / needs); // We are only as strong as our weakest point
                }
                else
                {
                    efficiency = 0.0f;
                }
            }

            Efficiency = efficiency;

            //-- Consume the resources
            if (efficiency > 0.0f)
            {
                foreach (var supply in _opex)
                {
                    float needs = supply.Value / annualDivider * efficiency;
                    allocator.Consume(supply.Key, cell, needs);
                }
            }

            //-- Produce the outputs
            foreach (var production in _output)
            {
                output[production.Key] = production.Value * efficiency / annualDivider;
            }
        }

        public override void DescribeDemand(Time currentTime, IDictionary<string, float> demand)
        {
            base.DescribeDemand(currentTime, demand);
            foreach (var supply in _opex)
            {
                demand[supply.Key] = supply.Value / currentTime.GetAnnualDivider();
            }
        }
    }
}
