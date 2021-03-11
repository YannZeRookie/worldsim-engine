using System;
using System.Collections.Generic;

namespace WorldSim.Engine
{
    public class JM2Source : JM2
    {
        protected string _resourceId;
        protected float? _reserve;
        protected float _production;
        protected IDictionary<string, object> _init;

        public JM2Source(IDictionary<string, object> init) : base()
        {
            Id = "source";
            _init = init;
            Restart();
        }

        public float? Reserve()
        {
            return _reserve;
        }

        public override void Restart()
        {
            _resourceId = _init["resource_id"] as string;
            _reserve = _init.ContainsKey("reserve") ? Convert.ToSingle(_init["reserve"]) : null;
            _production = Convert.ToSingle(_init["production"]);
            base.Restart();
        }

        public override void Step(Map map, IDictionary<string, float> stocks, Time currentTime, float annualDivider,
            IDictionary<string, float> output)
        {
            float productionTarget = _production / annualDivider;
            float produced = 0.0f;
            if (_reserve != null)
            {
                float reserve = (float) _reserve;
                produced = Math.Min(reserve, productionTarget);
                Efficiency = (produced < reserve) ? 1.0f : (reserve > 0.0f ? produced / productionTarget : 0.0f);
                _reserve -= produced;
            }
            else
            {
                // Infinite reserves
                produced = productionTarget;
                Efficiency = 1.0f;
            }

            output[_resourceId] = produced;
        }
    }
}
