using System;
using WorldSim.API;

namespace WorldSim.Model
{
    public class Resource : IResource
    {
        public Resource(string id, string name, string description, string type, IUnit? unit, string? distribution,
            int? range, float? attenuation)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = string.IsNullOrEmpty(type) ? "stock" : type;
            Unit = unit;
            Distribution = string.IsNullOrEmpty(distribution) ? "spread" : distribution;
            Range = range ?? 1;
            Attenuation = attenuation ?? 0.0f;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Distribution { get; set; }
        public int Range { get; set; }
        public float Attenuation { get; set; }

        public IUnit? Unit { get; set; }

        public string ValueToString(float value)
        {
            if (Unit == null)
                return string.Format("{0}:{1,10:0.0}", Name, value);
            return string.Format("{0}:{1,10:0.0} {2}", Name, value, Unit.Symbol);
        }

        public float ResourceToDemandConnection(Cell resCell, Cell demandCell)
        {
            switch (Distribution)
            {
                case "spread":
                    return 1.0f;
                case "radius":
                    return resCell.DistanceTo(demandCell) == Range ? 1.0f : 0.0f;
                case "local":
                    return resCell.DistanceTo(demandCell) <= Range ? 1.0f : 0.0f;
                case "attenuation":
                    var slope = Attenuation / (Range == 0 ? 1.0f : Range);
                    return Math.Max(0.0f, 1.0f - resCell.DistanceTo(demandCell) * slope);
                default:
                    throw new Exception("Unknown resource distribution: " + Distribution);
            }
        }
    }
}
