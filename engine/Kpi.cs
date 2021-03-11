using System;
using System.Linq;
using System.Text.RegularExpressions;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class Kpi : IKpi
    {
        private IUnit? _unit;
        private string _resourceId;
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
            Regex rx = new Regex(@"^sum\((\w+)\)$");
            MatchCollection matches = rx.Matches(formula);
            if (matches.Count == 1)
            {
                _resourceId = matches.First().Groups[1].Value;
            }
            else
            {
                throw new Exception("Error: could not understand formula: " + formula);
            }
        }

        public float GetValue(IMap map)
        {
            float result = 0.0f;
            foreach (var cell in map.Cells)
            {
                result += cell.GetStock(_resourceId);
            }

            return result;
        }

        public string ToString(IMap map, int padding)
        {
            float value = GetValue(map);
            string symbol = (_unit != null ? " " + _unit.Symbol : "");
            return String.Format("{0,-" + padding.ToString() + "}:{1,8:0.0}{2}", Name, value, symbol);
        }
    }
}
