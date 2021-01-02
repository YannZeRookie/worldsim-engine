using System;
using System.Collections.Generic;
using ChoETL;
using Mono.Options;
using WorldSim.Engine;

namespace cli
{
    class Program
    {
        static int Main(string[] args)
        {
            //-- Commande Line Processing, see https://github.com/xamarin/XamarinComponents/tree/master/XPlat/Mono.Options
            string fromDateOption = "";
            string toDateOption = "";
            var verbosity = 0;
            var shouldShowHelp = false;

            var options = new OptionSet
            {
                {"f|from=", "start date (YYYY-MM-DD). Implies -v", f => fromDateOption = f},
                {"t|to=", "end date (YYYY-MM-DD)", t => toDateOption = t},
                {
                    "v", "increase details verbosity:\n -v show each step\n -vv show each step and cells", v =>
                    {
                        if (v != null)
                            ++verbosity;
                    }
                },
                {"h|help", "show this message and exit", h => shouldShowHelp = (h != null)},
            };

            try
            {
                // Parse the command line
                var fileNames = options.Parse(args);
                int result = 0;
                if (shouldShowHelp)
                {
                    result = Help(options);
                }
                else
                {
                    if (fileNames.Count < 1) throw new Exception("Missing file name");
                    DateTime? fromDate = fromDateOption.IsEmpty() ? null : DateTime.Parse(fromDateOption);
                    DateTime? toDate = toDateOption.IsEmpty() ? null : DateTime.Parse(toDateOption);
                    // Now run
                    foreach (var fileName in fileNames)
                    {
                        result += RunSimulation(fileName, fromDate, toDate, verbosity);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                // output some error message
                Console.Write("Error: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try '--help' for more information.");
                return 1;
            }
        }

        private static int Help(OptionSet options)
        {
            Console.WriteLine("Usage: engine.exe [OPTIONS]+ fileName");
            Console.WriteLine("Run a simulation until the `to` date and show the resulting KPIs.");
            Console.WriteLine("If no `to` date is specified, the `currentTime` from the file is used.");
            Console.WriteLine("If a `from` date is specified, KPIs will be shown for each step.");
            Console.WriteLine();

// output the options
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
            return 0;
        }

        public static int RunSimulation(string fileName, DateTime? fromDate, DateTime? toDate, int verbosity)
        {
            Engine engine = new Engine();
            if (fileName.Length == 0) throw new Exception("no file specified");
            engine.LoadYaml(fileName);

            DateTime reachDate = toDate ?? engine.World.Time.Current;
            
            if (fromDate != null)
            {
                if (fromDate < engine.World.Time.Start)
                    throw new Exception(String.Format("`from` date ({0:yyyy-MM-dd}) cannot be lower than the simulation start date ({1:yyyy-MM-dd})", fromDate, engine.World.Time.Start));
                engine.World.Time.Current = (DateTime) fromDate; // Yes, the current date, NOT the starting one
                verbosity = Math.Max(1, verbosity);
            }

            
            if (reachDate < engine.World.Time.Start)
                throw new Exception(String.Format("`to` date ({0:yyyy-MM-dd}) cannot be lower than the simulation start date ({1:yyyy-MM-dd})", reachDate, engine.World.Time.Start));
            if (fromDate != null && reachDate < fromDate)
                throw new Exception(String.Format("`to` date ({0:yyyy-MM-dd}) cannot be lower than `from` date ({1:yyyy-MM-dd})", reachDate, fromDate));

            if (reachDate < engine.World.Time.Current || (fromDate == null && verbosity >= 1)) // In these cases we ignore the Current date and we rewind
                engine.World.Time.Restart();
            while (!engine.World.Time.Reached(reachDate))
            {
                if (verbosity >= 1)
                {
                    PrintCurrent(engine, verbosity);
                }

                engine.World.Time.Step();
            }

            PrintCurrent(engine, verbosity);
            return 0;
        }

        private static void PrintCurrent(Engine engine, int verbosity)
        {
            Console.Write("#{0,3}: {1:yyyy-MM-dd}: ", engine.World.Time.Iteration, engine.World.Time.Current);
            foreach (var kpi in engine.World.KeyAttributes)
            {
                Console.Write(kpi.ToString() + " ");
            }

            Console.WriteLine();
            if (verbosity >= 2)
            {
                foreach (var cell in engine.World.Map.Cells)
                {
                    Console.WriteLine(" " + cell.ToString());
                }

                Console.WriteLine();
            }
        }
    }
}