using WorldSim.API;

namespace WorldSim.Engine
{
    public class Resource : IResource
    {
        public Resource(string id, string name, string description, string type, string unitId)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Type = type;
            this.UnitId = unitId;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string UnitId { get; set; }
    }
}