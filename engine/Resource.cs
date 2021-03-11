using System;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class Resource : IResource
    {
        private IUnit _unit;

        public IUnit Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }

        public Resource(string id, string name, string description, string type, IUnit? unit)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Type = type;
            this._unit = unit;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }

        public string ValueToString(float value)
        {
            if (Unit == null)
                return String.Format("{0}:{1,10:0.0}", Name, value);
            else
                return String.Format("{0}:{1,10:0.0} {2}", Name, value, Unit.Symbol);
        }
    }
}
