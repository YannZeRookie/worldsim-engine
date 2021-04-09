using System;
using WorldSim.API;
using WorldSim.IO;
using WorldSim.Model;

namespace WorldSim.Engine
{
    public class Engine
    {
        private World _world;

        public IWorld World => _world;

        public TimeSpan LoadDelay { get; set; }
        public TimeSpan CurrentStateDelay { get; set; }

        public Engine()
        {
            _world = new World();
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
            Importer importer = new Importer(_world, fileName);
            DateTime currentTime = importer.LoadYaml(dontRun);
            LoadDelay = importer.LoadDelay;
            CurrentStateDelay = importer.CurrentStateDelay;
            return currentTime;
        }
    }
}
