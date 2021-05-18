using System;
using System.Globalization;
using WorldSim.API;

namespace WorldSim.Model
{
    public abstract class Kpi : IKpi
    {
        protected IWorld World;
        
        public Kpi(IWorld world, string name, string description, string formula, IUnit? unit)
        {
            World = world;
            Name = name;
            Description = description;
            Formula = formula;
            Unit = unit;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Formula { get; set; }

        public IUnit Unit { get; set; }

        public abstract float GetValue();

        public string ToString(int padding)
        {
            var value = GetValue();
            var symbol = Unit != null ? " " + Unit.Symbol : "";
            return string.Format("{0,-" + padding + "}:{1,8:0.0}{2}", Name, value, symbol);
        }
    }

    internal class KpiResourceSum : Kpi
    {
        private readonly string _resourceId;

        public KpiResourceSum(IWorld world, string name, string description, string formula, IUnit? unit, string resourceId) :
            base(world, name, description, formula, unit)
        {
            _resourceId = resourceId;
        }

        public override float GetValue()
        {
            var result = 0.0f;
            foreach (var cell in World.Map.Cells) result += cell.GetStock(_resourceId);

            return result;
        }
    }

    internal class KpiIteration : Kpi
    {
        public KpiIteration(IWorld world, string name, string description, string formula, IUnit? unit) :
            base(world, name, description, formula, unit)
        {
        }

        public override float GetValue()
        {
            return Convert.ToSingle(World.Time.Iteration, CultureInfo.InvariantCulture);
        }
    }
}
