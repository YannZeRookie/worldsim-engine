using System;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Model
{
    public class JM2Source : JM2
    {
        protected string _resourceId;
        protected float? _reserve;
        protected float _production;
        protected float _produced = 0.0f;

        public JM2Source(DataDictionary init) : base(init)
        {
            Id = "source";
            Restart();
        }

        public float? Reserve()
        {
            return _reserve;
        }

        protected override DataDictionary GetValues()
        {
            DataDictionary result = new DataDictionary()
            {
                {"resource_id", _resourceId},
                {"production", _production},
                {"produced", _produced}
            };
            if (_reserve != null) result.Add("reserve", (float) _reserve);
            return result;
        }

        public override void Restart()
        {
            _resourceId = _init["resource_id"].StringValue;
            _reserve = null;
            if (_init.ContainsKey("reserve"))
            {
                _reserve = _init["reserve"].FloatValue;
            }

            _production = _init["production"].FloatValue;
            base.Restart();
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            float annualDivider = currentTime.GetAnnualDivider();
            float productionTarget = _production / annualDivider;
            _produced = 0.0f;
            if (_reserve != null)
            {
                float reserve = (float) _reserve;
                _produced = Math.Min(reserve, productionTarget);
                Efficiency = (_produced < reserve) ? 1.0f : (reserve > 0.0f ? _produced / productionTarget : 0.0f);
                _reserve -= _produced;
            }
            else
            {
                // Infinite reserves
                _produced = productionTarget;
                Efficiency = 1.0f;
            }

            output[_resourceId] = _produced;
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

        public Jm2SourceMinMax(DataDictionary init) : base(init)
        {
            Id = "sourceMinMax";
        }

        public override void Step(IDictionary<string, float> stocks, Time currentTime,
            Allocator allocator, Cell cell, IDictionary<string, float> output)
        {
            if (stocks[_resourceId] >= _init["levelMax"].FloatValue)
            {
                _active = false;
            }

            if (!_active && stocks[_resourceId] < _init["levelMin"].FloatValue)
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
                _produced = 0.0f;
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
