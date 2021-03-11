using System;
using System.Collections.Generic;

namespace WorldSim.Engine
{
    public class JM2Sink : JM2
    {
        private string _resourceId;
        private float? _limit;
        private float _consumption;
        private IDictionary<string, object> _init;

        public JM2Sink(IDictionary<string, object> init) : base()
        {
            Id = "sink";
            _init = init;
            Restart();
        }

        public override void Restart()
        {
            _resourceId = _init["resource_id"] as string;
            _limit = _init.ContainsKey("limit") ? Convert.ToSingle(_init["limit"]) : null;
            _consumption = Convert.ToSingle(_init["consumption"]);
            base.Restart();
        }

        public override void Step(Map map, Dictionary<string, float> stocks, Time currentTime, float annualDivider,
            Dictionary<string, float> output)
        {
            float consumptionTarget = _consumption / annualDivider;
            float actualTarget = Math.Min(_limit ?? _consumption, _consumption) / annualDivider;
            float consumed = 0.0f;
            float efficiency = 1.0f;

            consumed = ConsumeResource(_resourceId, actualTarget, map, stocks);
            if (consumptionTarget > 0.0f)
                efficiency = consumed / consumptionTarget;

            Efficiency = efficiency;
        }
    }
}