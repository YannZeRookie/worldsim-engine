using WorldSim.API;

namespace WorldSim.Model
{
    public class Unit : IUnit
    {
        public Unit(string id, string name, string description, string symbol)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Symbol = symbol;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Symbol { get; set; }
    }
}