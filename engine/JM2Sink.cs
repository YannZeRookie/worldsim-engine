using System;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Model
{
    public class JM2Sink : JM2
    {
        private string _resourceId;
        private float? _limit;
        private float _consumption;
        private float _consumed = 0.0f;

        public JM2Sink(DataDictionary init) : base(init)
        {
            Id = "sink";
            Restart();
        }

        protected override DataDictionary GetValues()
        {
            DataDictionary result = new DataDictionary()
            {
                {"resource_id", _resourceId},
                {"consumption", _consumption},
                {"consumed", _consumed}
            };
            if (_limit != null) result.Add("limit", (float) _limit);
            return result;
        }

        public override void Restart()
        {
            _resourceId = _init["resource_id"].StringValue;
            _limit = null;
            if (_init.ContainsKey("limit"))
            {
                _limit = _init["limit"].FloatValue;
            }
            _consumption = _init["consumption"].FloatValue;
            base.Restart();
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            float annualDivider = currentTime.GetAnnualDivider();
            float consumptionTarget = _consumption / annualDivider;
            float actualTarget = Math.Min(_limit ?? _consumption, _consumption) / annualDivider;
            _consumed = 0.0f;
            float efficiency = 1.0f;

            _consumed = allocator.Consume(_resourceId, cell, actualTarget);
            _limit -= _consumed;

            if (consumptionTarget > 0.0f)
                efficiency = _consumed / consumptionTarget;

            Efficiency = efficiency;
        }

        public override void DescribeDemand(Time currentTime, IDictionary<string, float> demand)
        {
            base.DescribeDemand(currentTime, demand);
            demand[_resourceId] = _consumption / currentTime.GetAnnualDivider();
        }
    }
}
