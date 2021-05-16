using System;
using System.Collections.Generic;
using System.Globalization;

namespace WorldSim.Model
{
    public class JM2Sink : JM2
    {
        private string _resourceId;
        private float? _limit;
        private float _consumption;

        public JM2Sink(IDictionary<string, object> init) : base(init)
        {
            Id = "sink";
            Restart();
        }

        public override void Restart()
        {
            _resourceId = _init["resource_id"] as string;
            _limit = null;
            if (_init.ContainsKey("limit"))
            {
                _limit = Convert.ToSingle(_init["limit"], CultureInfo.InvariantCulture);
            }
            _consumption = Convert.ToSingle(_init["consumption"], CultureInfo.InvariantCulture);
            base.Restart();
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            float annualDivider = currentTime.GetAnnualDivider();
            float consumptionTarget = _consumption / annualDivider;
            float actualTarget = Math.Min(_limit ?? _consumption, _consumption) / annualDivider;
            float consumed = 0.0f;
            float efficiency = 1.0f;

            consumed = allocator.Consume(_resourceId, cell, actualTarget);

            if (consumptionTarget > 0.0f)
                efficiency = consumed / consumptionTarget;

            Efficiency = efficiency;
        }

        public override void DescribeDemand(Time currentTime, IDictionary<string, float> demand)
        {
            base.DescribeDemand(currentTime, demand);
            demand[_resourceId] = _consumption / currentTime.GetAnnualDivider();
        }
    }
}
