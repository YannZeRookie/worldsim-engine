using System;
using WorldSim.IO;

namespace WorldSim.Engine
{
    public class Engine
    {
        public World World { get; }
        public TimeSpan LoadDelay { get; set; }
        public TimeSpan CurrentStateDelay { get; set; }

        public Engine()
        {
            this.World = new World();
        }

        /// <summary>
        /// Load a YAML file into the World
        /// </summary>
        /// <param name="fileName">File path name</param>
        /// <param name="dontRun">By default, the simulation is automatically run
        /// until the Current Time indicated in the file (if any). This prevents it, the
        /// simulation Current Time being set at the Start Time.</param>
        /// <returns>The `currentTime` that was indicated in the file, or the Start Time by default.</returns>
        public DateTime LoadYaml(string fileName, bool dontRun = false)
        {
            Importer importer = new Importer(this.World, fileName);
            DateTime currentTime = importer.LoadYaml(dontRun);
            LoadDelay = importer.LoadDelay;
            CurrentStateDelay = importer.CurrentStateDelay;
            return currentTime;
        }
    }
}