using System;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Model
{
    public class JM2Factory : JM2
    {
        private Dictionary<string, float> _opex;
        private Dictionary<string, float> _output;
        private Dictionary<string, float> _produced; // Copy of what was produced    
        
        public JM2Factory(DataDictionary init) : base(init)
        {
            Id = "factory";
            _opex = new Dictionary<string, float>();
            _output = new Dictionary<string, float>();
            _produced = new Dictionary<string, float>();
            Restart();
        }

        public override void Restart()
        {
            _opex.Clear();
            if (_init.ContainsKey("opex") && _init["opex"] is DataDictionary opex)
            {
                foreach (var op in opex)
                {
                    _opex[op.Key] = op.Value.FloatValue;
                }
            }

            _output.Clear();
            if (_init.ContainsKey("output") && _init["output"] is DataDictionary output)
            {
                foreach (var ot in output)
                {
                    _output[ot.Key] = ot.Value.FloatValue;
                }
            }

            base.Restart();
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            _produced.Clear();
            var annualDivider = currentTime.GetAnnualDivider();
            //-- Compute the expected efficiency
            var efficiency = 1.0f;
            foreach (var supply in _opex)
                // Check what was allocated to us and compare to our needs
                if (efficiency > 0.0f)
                {
                    var needs = supply.Value / annualDivider;
                    if (needs > 0.0f)
                        efficiency =
                            Math.Min(efficiency,
                                allocator.GetAllocation(supply.Key, cell) /
                                needs); // We are only as strong as our weakest point
                }
                else
                {
                    efficiency = 0.0f;
                }

            Efficiency = efficiency;

            //-- Consume the resources
            if (efficiency > 0.0f)
                foreach (var supply in _opex)
                {
                    var needs = supply.Value / annualDivider * efficiency;
                    allocator.Consume(supply.Key, cell, needs);
                }

            //-- Produce the outputs
            foreach (var production in _output)
            {
                float produced = production.Value * efficiency / annualDivider;
                output[production.Key] = produced;
                _produced[production.Key] = produced;
            }
        }
        
        protected override DataDictionary GetValues()
        {
            DataDictionary result = (DataDictionary) _init.Clone();
            result.Add("produced", _produced);
            return result;
        }

        public override void DescribeDemand(Time currentTime, IDictionary<string, float> demand)
        {
            base.DescribeDemand(currentTime, demand);
            foreach (var supply in _opex) demand[supply.Key] = supply.Value / currentTime.GetAnnualDivider();
        }
    }
}
