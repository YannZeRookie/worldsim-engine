using WorldSim.IO;
    
namespace WorldSim.Engine
{
    public class Engine
    {
        public World World { get; }

        public Engine()
        {
            this.World = new World();
        }

        public void LoadYaml(string fileName)
        {
            Importer importer = new Importer(this.World, fileName);
            importer.LoadYaml();
        }
    }
}