using System;
using ChoETL;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class Resource : IResource
    {
        private IUnit? _unit;
        public IUnit? Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }
        public int? Range { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Distribution { get; set; }

        public Resource(string id, string name, string description, string type, IUnit? unit, string? distribution, int? range)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Type = type.IsNullOrEmpty() ? "stock" : type;
            this._unit = unit;
            this.Distribution = distribution.IsNullOrEmpty() ? "spread" : distribution;
            this.Range = range.IsNull() ? 1 : range;
        }

        public float ResourceToDemandConnection(Cell resCell, Cell demandCell)
        {
            switch (Distribution)
            {
                case "spread":
                    return 1.0f;
                case "local":
                    return resCell.DistanceTo(demandCell) <= Range ? 1.0f : 0.0f;
                default:
                    throw new Exception("Unknown resource distribution: " + Distribution);
            }
        }

        public string ValueToString(float value)
        {
            if (_unit == null)
                return String.Format("{0}:{1,10:0.0}", Name, value);
            else
                return String.Format("{0}:{1,10:0.0} {2}", Name, value, Unit.Symbol);
        }
    }
}
