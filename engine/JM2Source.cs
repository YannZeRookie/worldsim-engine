using System;
using System.Collections.Generic;
using System.Globalization;

namespace WorldSim.Model
{
    public class JM2Source : JM2
    {
        protected string _resourceId;
        protected float? _reserve;
        protected float _production;

        public JM2Source(IDictionary<string, object> init) : base(init)
        {
            Id = "source";
            Restart();
        }

        public float? Reserve()
        {
            return _reserve;
        }

        public override void Restart()
        {
            _resourceId = _init["resource_id"] as string ?? string.Empty;
            _reserve = null;
            if (_init.ContainsKey("reserve"))
            {
                _reserve = Convert.ToSingle(_init["reserve"], CultureInfo.InvariantCulture);
            }

            _production = Convert.ToSingle(_init["production"], CultureInfo.InvariantCulture);
            base.Restart();
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            float annualDivider = currentTime.GetAnnualDivider();
            float productionTarget = _production / annualDivider;
            float produced = 0.0f;
            if (_reserve != null)
            {
                float reserve = (float)_reserve;
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

        public override string GetExtraLine(int extraLine)
        {
            if (_reserve != null)
            {
                switch (extraLine)
                {
                    case 0:
                        return GetExtraLine0();
                    case 1:
                        return " Reserve: " + _reserve.ToString();
                }
            }

            return "";
        }

        public override int NbExtraLines()
        {
            return _reserve != null ? 2 : 0;
        }

        public override int ExtraWidth()
        {
            return 20;
        }
    }

    /// <summary>
    /// A Source with a minimum and a maximum stock level.
    /// If the Cell's stock reaches the maximum level, production stops.
    /// If the Cell's stock goes below the minimum level, production resumes.
    /// </summary>
    public class Jm2SourceMinMax : JM2Source
    {
        private bool _active = false; // Is production active or not?

        public Jm2SourceMinMax(IDictionary<string, object> init) : base(init)
        {
            Id = "sourceMinMax";
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            if (stocks[_resourceId] >= Convert.ToSingle(_init["levelMax"], CultureInfo.InvariantCulture))
            {
                _active = false;
            }

            if (!_active && stocks[_resourceId] < Convert.ToSingle(_init["levelMin"], CultureInfo.InvariantCulture))
            {
                _active = true;
            }

            if (_active)
            {
                base.Step(stocks, currentTime, allocator, cell, output);
            }
            else
            {
                output[_resourceId] = 0.0f;
            }
        }

        public override string GetExtraLine(int extraLine)
        {
            {
                switch (extraLine)
                {
                    case 0:
                        return GetExtraLine0();
                    case 1:
                        return _active ? " Active " : " Inactive";
                    case 2:
                        if (_reserve != null)
                            return " Reserve: " + _reserve.ToString();
                        else
                            return "";
                }
            }

            return "";
        }

        public override int NbExtraLines()
        {
            return 2 + (_reserve != null ? 1 : 0);
        }

        public override int ExtraWidth()
        {
            return 20;
        }
    }
}
