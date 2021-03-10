using System;
using System.Linq;
using System.Text.RegularExpressions;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class Kpi : IKpi
    {
        private World _world;
        private string _resourceID;
        public string Name { get; set; }
        public string Description { get; set; }
        public string Formula { get; set; }
        public string UnitId { get; set; }

        public Kpi(string name, string description, string formula, string unitId, IWorld world)
        {
            Name = name;
            Description = description;
            Formula = formula;
            UnitId = unitId;
            _world = (World) world;
            Regex rx = new Regex(@"^sum\((\w+)\)$");
            MatchCollection matches = rx.Matches(formula);
            if (matches.Count == 1)
            {
                _resourceID = matches.First().Groups[1].Value;
            }
            else
            {
                throw new Exception("Error: could not understand formula: " + formula);
            }
        }

        public float GetValue()
        {
            float result = 0.0f;
            foreach (var cell in _world.Map.Cells)
            {
                result += cell.GetStock(_resourceID);
            }

            return result;
        }

        public string ToString(int padding)
        {
            float value = GetValue();
            string symbol = _world.Units[UnitId].Symbol;
            return String.Format("{0,-" + padding.ToString() + "}:{1,8:0.0} {2}", Name, value, symbol);
        }
    }
}