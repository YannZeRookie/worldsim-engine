using System;
using System.Globalization;
using WorldSim.API;

namespace WorldSim.Model
{
    public abstract class Kpi : IKpi
    {
        public Kpi(string name, string description, string formula, IUnit? unit)
        {
            Name = name;
            Description = description;
            Formula = formula;
            Unit = unit;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Formula { get; set; }

        public IUnit Unit { get; set; }

        public abstract float GetValue(IWorld world);

        public string ToString(IWorld world, int padding)
        {
            var value = GetValue(world);
            var symbol = Unit != null ? " " + Unit.Symbol : "";
            return string.Format("{0,-" + padding + "}:{1,8:0.0}{2}", Name, value, symbol);
        }
    }

    internal class KpiResourceSum : Kpi
    {
        private readonly string _resourceId;

        public KpiResourceSum(string name, string description, string formula, IUnit? unit, string resourceId) :
            base(name, description, formula, unit)
        {
            _resourceId = resourceId;
        }

        public override float GetValue(IWorld world)
        {
            var result = 0.0f;
            foreach (var cell in world.Map.Cells) result += cell.GetStock(_resourceId);

            return result;
        }
    }

    internal class KpiIteration : Kpi
    {
        public KpiIteration(string name, string description, string formula, IUnit? unit) :
            base(name, description, formula, unit)
        {
        }

        public override float GetValue(IWorld world)
        {
            return Convert.ToSingle(world.Time.Iteration, CultureInfo.InvariantCulture);
        }
    }
}
