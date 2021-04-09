using System;
using System.Linq;
using System.Text.RegularExpressions;
using WorldSim.API;

namespace WorldSim.Model
{
    public abstract class Kpi : IKpi
    {
        private IUnit? _unit;
        public string Name { get; set; }
        public string Description { get; set; }
        public string Formula { get; set; }

        public IUnit Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }

        public Kpi(string name, string description, string formula, IUnit? unit)
        {
            Name = name;
            Description = description;
            Formula = formula;
            _unit = unit;
        }

        public abstract float GetValue(IWorld world);

        public string ToString(IWorld world, int padding)
        {
            float value = GetValue(world);
            string symbol = (_unit != null ? " " + _unit.Symbol : "");
            return String.Format("{0,-" + padding.ToString() + "}:{1,8:0.0}{2}", Name, value, symbol);
        }
    }

    class KpiResourceSum : Kpi
    {
        private string _resourceId;

        public KpiResourceSum(string name, string description, string formula, IUnit? unit, string resourceId) :
            base(name, description, formula, unit)
        {
            _resourceId = resourceId;
        }

        public override float GetValue(IWorld world)
        {
            float result = 0.0f;
            foreach (var cell in world.Map.Cells)
            {
                result += cell.GetStock(_resourceId);
            }

            return result;
        }
    }

    class KpiIteration : Kpi
    {
        public KpiIteration(string name, string description, string formula, IUnit? unit) :
            base(name, description, formula, unit)
        {
        }

        public override float GetValue(IWorld world)
        {
            return Convert.ToSingle(world.Time.Iteration);
        }
    }
}
