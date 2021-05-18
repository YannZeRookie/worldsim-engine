using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace WorldSim.Model
{
    public class JM2Factory : JM2
    {
        private readonly Dictionary<string, float> _opex;
        private readonly Dictionary<string, float> _output;

        public JM2Factory(IDictionary<string, object> init) : base(init)
        {
            Id = "factory";
            _opex = new Dictionary<string, float>();
            _output = new Dictionary<string, float>();
            Restart();
        }

        public override void Restart()
        {
            _opex.Clear();
            if (_init.ContainsKey("opex") && _init["opex"] is IDictionary<object, object> opex)
            {
                foreach (var op in opex)
                {
                    _opex[(string) op.Key] = Convert.ToSingle(op.Value, CultureInfo.InvariantCulture);
                }
            }

            _output.Clear();
            if (_init.ContainsKey("output") && _init["output"] is IDictionary<object, object> output)
            {
                foreach (var ot in output)
                {
                    _output[(string) ot.Key] = Convert.ToSingle(ot.Value, CultureInfo.InvariantCulture);
                }
            }

            base.Restart();
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
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
            foreach (var production in _output) output[production.Key] = production.Value * efficiency / annualDivider;
        }

        public override void DescribeDemand(Time currentTime, IDictionary<string, float> demand)
        {
            base.DescribeDemand(currentTime, demand);
            foreach (var supply in _opex) demand[supply.Key] = supply.Value / currentTime.GetAnnualDivider();
        }
    }
}
