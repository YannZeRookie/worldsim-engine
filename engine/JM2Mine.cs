using System;
using System.Collections.Generic;

namespace WorldSim.Engine
{
    public class JM2Mine : JM2Source
    {
        public JM2Mine(IDictionary<string, object> init) : base(init)
        {
            Id = "mine";
        }
    }

    public class JM2RecyclingMine : JM2Mine
    {
        private float _recycling = 0.0f;
        private Int32 _timeInUse = 1;
        private Dictionary<Int32, float> _history;

        public JM2RecyclingMine(IDictionary<string, object> init) : base(init)
        {
            Id = "recycling_mine";
            _history = new Dictionary<Int32, float>();
        }

        public override void Restart()
        {
            base.Restart();
            _recycling = _init.ContainsKey("recycling") ? Convert.ToSingle(_init["recycling"]) : 0.0f;
            _timeInUse = _init.ContainsKey("time_in_use") ? Convert.ToInt32(_init["time_in_use"]) : 1;
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            IDictionary<string, Allocation> allocations, IDictionary<string, float> output)
        {
            float annualDivider = currentTime.GetAnnualDivider();
            float productionTarget = _production / annualDivider;
            float extracted = 0.0f;
            float produced = 0.0f;
            float recycled = 0.0f;
            float reserve = (float) _reserve;

            if (currentTime.Iteration >= _timeInUse)
            {
                if (_history.ContainsKey(currentTime.Iteration - _timeInUse))
                {
                    recycled = _recycling * _history[currentTime.Iteration - _timeInUse];
                }
                else
                {
                    recycled = _recycling * _production;    // Poor man's solution
                }
            }
            extracted = Math.Min(reserve, productionTarget - recycled);
            _reserve -= extracted;
            produced = extracted + recycled;

            Efficiency = (extracted < reserve) ? 1.0f : (reserve > 0.0f ? produced / productionTarget : 0.0f);
            _history[currentTime.Iteration] = produced;
            output[_resourceId] = produced;
        }
    }
}
