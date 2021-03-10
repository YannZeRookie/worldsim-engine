using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using ChoETL;
using Mono.Options;
using WorldSim.API;
using WorldSim.Engine;

namespace cli
{
    class Program
    {
        static int Main(string[] args)
        {
            //-- Command Line Processing, see https://github.com/xamarin/XamarinComponents/tree/master/XPlat/Mono.Options
            string fromDateOption = "";
            string toDateOption = "";
            var verbosity = 0;
            var graphic = false;
            var delay = "0";
            var interactive = false;
            var csvOutput = false;
            var shouldShowHelp = false;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

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
                {"g|graphic", "Use the 'graphic' display mode", g => graphic = (g != null)},
                {"d|delay=", "Delay in 1000th of secs between steps in -vv mode", d => delay = d},
                {"i|interactive", "Use arrow keys to progress", i => interactive = (i != null)},
                {"c|csv", "Output results in a CSV file", c => csvOutput = (c != null)},
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
                    if (graphic) Console.Clear();
                    // Now run
                    foreach (var fileName in fileNames)
                    {
                        result += RunSimulation(fileName, fromDate, toDate, verbosity, graphic, Int32.Parse(delay),
                            interactive, csvOutput);
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
            Console.WriteLine("Usage: engine [OPTIONS]+ fileName");
            Console.WriteLine("Run a simulation until the `to` date and show the resulting KPIs.");
            Console.WriteLine("If no `to` date is specified, the `currentTime` from the file is used.");
            Console.WriteLine("If a `from` date is specified, details will be displayed from this date.");
            Console.WriteLine();

            // output the options
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
            return 0;
        }

        public static int RunSimulation(string fileName, DateTime? fromDate, DateTime? toDate, int verbosity,
            bool graphic, Int32 delay, bool interactive, bool csvOutput)
        {
            Engine engine = new Engine();
            if (fileName.Length == 0) throw new Exception("no file specified");

            DateTime currentTime = engine.LoadYaml(fileName, true);
            Console.WriteLine("Loaded file in {0} ms", engine.LoadDelay.Milliseconds);

            DateTime reachDate = toDate ?? currentTime;

            if (fromDate != null)
            {
                if (fromDate < engine.World.Time.Start)
                    throw new Exception(String.Format(
                        "`from` date ({0:yyyy-MM-dd}) cannot be lower than the simulation start date ({1:yyyy-MM-dd})",
                        fromDate, engine.World.Time.Start));
                engine.World.Time.Current = (DateTime) fromDate; // Yes, the current date, NOT the starting one
                verbosity = Math.Max(1, verbosity);
            }

            if (toDate != null)
            {
                if (toDate < engine.World.Time.Start)
                    throw new Exception(String.Format(
                        "`to` date ({0:yyyy-MM-dd}) cannot be lower than the simulation start date ({1:yyyy-MM-dd})",
                        toDate, engine.World.Time.Start));
            }

            if (fromDate != null && reachDate < fromDate)
                throw new Exception(String.Format(
                    "the target date ({0:yyyy-MM-dd}) cannot be lower than `from` date ({1:yyyy-MM-dd})", reachDate,
                    fromDate));

            if (verbosity <= 0) // No step details to show? Fast-forward directly to target!
                engine.World.Time.Current = reachDate;

            ChoCSVWriter csvWriter = null;
            if (csvOutput)
            {
                csvWriter = new ChoCSVWriter(fileName + ".csv");
                csvWriter.WithFirstLineHeader();
                string[] fields =
                    new string[2 + engine.World.Kpis.Count + (verbosity > 1
                        ? engine.World.Map.SizeX * engine.World.Map.SizeY * engine.World.Resources.Count
                        : 0)];
                int i = 0;
                fields[i++] = "#";
                fields[i++] = "Date";
                foreach (var kpi in engine.World.Kpis)
                {
                    fields[i++] = kpi.Name;
                }

                for (int y = 0; y < engine.World.Map.SizeY; y++)
                {
                    for (int x = 0; x < engine.World.Map.SizeX; x++)
                    {
                        foreach (var resource in engine.World.Resources.Values)
                        {
                            fields[i++] = "[" + x + ":" + y + "] " + resource.Name;
                        }
                    }
                }

                csvWriter.WriteHeader(fields);
            }

            //-- Compute the maximum widths. TODO: move this code higher up
            int resNamesWidth = 0;
            int unitNamesWidth = 0;
            int resWidth = 8;
            foreach (var resource in engine.World.Resources.Values)
            {
                if (resource.Name.Length > resNamesWidth) resNamesWidth = resource.Name.Length;
                if (engine.World.Units[resource.UnitId].Symbol.Length > unitNamesWidth)
                    unitNamesWidth = engine.World.Units[resource.UnitId].Symbol.Length;
            }

            Dictionary<string, int> widths = new Dictionary<string, int>()
                {{"resNames", resNamesWidth}, {"unitNames", unitNamesWidth}, {"res", resWidth}};

            (int, int) startPosition = Console.GetCursorPosition();

            //-- Main loop
            while (!engine.World.Time.Reached(reachDate) || interactive)
            {
                if (verbosity >= 1)
                {
                    PrintCurrent(engine, verbosity, graphic, startPosition, widths, csvWriter);
                }

                if (interactive)
                {
                    ConsoleKeyInfo cki = Console.ReadKey(true);
                    if ((cki.Key == ConsoleKey.LeftArrow) || (cki.Key == ConsoleKey.PageUp))
                    {
                        // Go Backward
                        engine.World.Time.StepBack();
                    }
                    else if ((cki.Key == ConsoleKey.UpArrow) || (cki.Key == ConsoleKey.Home))
                    {
                        // Go to Start
                        engine.World.Time.Restart();
                    }
                    else if ((cki.Key == ConsoleKey.DownArrow) || (cki.Key == ConsoleKey.End))
                    {
                        // Go to End
                        engine.World.Time.Current = engine.World.Time.End;
                    }
                    else if ((cki.Key == ConsoleKey.T))
                    {
                        // Go to target date
                        engine.World.Time.Current = reachDate;
                    }
                    else if ((cki.Key == ConsoleKey.Escape) || (cki.Key == ConsoleKey.Q))
                    {
                        //Exit
                        break;
                    }
                    else
                    {
                        engine.World.Time.Step();
                    }
                }
                else
                {
                    if (delay > 0) Thread.Sleep(delay);
                    engine.World.Time.Step();
                }
            }

            PrintCurrent(engine, verbosity, graphic, startPosition, widths, csvWriter);

            if (csvWriter != null) csvWriter.Close();
            return 0;
        }

        private static void PrintCurrent(Engine engine, int verbosity,
            bool graphic, (int, int) startPosition, IDictionary<string, int> widths, ChoCSVWriter csvWriter)
        {
            dynamic csvRecord = new ExpandoObject();

            if (graphic) Console.SetCursorPosition(startPosition.Item1, startPosition.Item2);
            //-- Iteration and date
            Console.Write("#{0,4}: {1:yyyy-MM-dd}  ", engine.World.Time.Iteration, engine.World.Time.Current);
            if (graphic)
                PrintProgressBar(engine.World.Time.Start.Ticks, engine.World.Time.Current.Ticks,
                    engine.World.Time.End.Ticks);
            Console.WriteLine();
            csvRecord.Iteration = engine.World.Time.Iteration;
            csvRecord.Date = engine.World.Time.Current.ToString("yyyy-MM-dd");
            //-- KPIs
            int kpiMaxWdith = engine.World.GetKpisMaxWidth();
            foreach (var kpi in engine.World.Kpis)
            {
                Console.WriteLine(kpi.ToString(kpiMaxWdith));
                ((IDictionary<String, Object>) csvRecord).Add(kpi.Name, kpi.GetValue());
            }

            Console.WriteLine();
            //-- Cells
            if (verbosity >= 2)
            {
                if (graphic)
                    PrintMapGraphic(engine.World, widths);
                else
                    PrintMapText(engine.World, widths);
                if (csvWriter != null)
                {
                    for (int y = 0; y < engine.World.Map.SizeY; y++)
                    {
                        for (int x = 0; x < engine.World.Map.SizeX; x++)
                        {
                            ICell cell = engine.World.Map.Cells[x, y];
                            foreach (var resource in engine.World.Resources.Values)
                            {
                                ((IDictionary<String, Object>) csvRecord).Add(x + ":" + y + " " + resource.Id,
                                    cell.GetStock(resource.Id));
                            }
                        }
                    }
                }
            }

            //--
            if (csvWriter != null) csvWriter.Write(csvRecord);
        }

        private static void PrintMapText(IWorld world, IDictionary<string, int> widths)
        {
            for (int y = 0; y < world.Map.SizeY; y++)
            {
                for (int x = 0; x < world.Map.SizeX; x++)
                {
                    Console.WriteLine(" " + world.Map.Cells[x, y].ToString());
                }
            }

            Console.WriteLine();
        }

        private static void PrintMapGraphic(IWorld world, IDictionary<string, int> widths)
        {
            int resNamesWidth = widths["resNames"];
            int unitNamesWidth = widths["unitNames"];
            int resWidth = widths["res"];

            //-- Compute the maximum widths. TODO: move this code higher up
            foreach (var resource in world.Resources.Values)
            {
                if (resource.Name.Length > resNamesWidth) resNamesWidth = resource.Name.Length;
                if (world.Units[resource.UnitId].Symbol.Length > unitNamesWidth)
                    unitNamesWidth = world.Units[resource.UnitId].Symbol.Length;
            }

            int cellWidth = 1 + resNamesWidth + 2 + resWidth + 1 + unitNamesWidth + 2;

            //-- Now draw the table
            for (int y = 0; y < world.Map.SizeY; y++)
            {
                //-- Cell Headers
                for (int x = 0; x < world.Map.SizeX; x++)
                {
                    ICell cell = world.Map.Cells[x, y];
                    Char corner = Corner(x, y, world.Map.SizeX, world.Map.SizeY);
                    string cellNo = String.Format("{0},{1}\u2500", x, y);
                    string jm2 = (cell.Jm2 != null) ? cell.Jm2.Id + '\u2500' + Efficiency(cell) : "\u2500\u2500";
                    string headerPadding = new String('\u2500', cellWidth - 1 - cellNo.Length - jm2.Length);
                    Console.Write(corner + cellNo + jm2 + headerPadding);
                }

                Console.WriteLine(Corner(world.Map.SizeX, y, world.Map.SizeX, world.Map.SizeY));

                //-- Cell Stocks
                foreach (var resource in world.Resources.Values)
                {
                    for (int x = 0; x < world.Map.SizeX; x++)
                    {
                        ICell cell = world.Map.Cells[x, y];
                        string col = "\u2502 ";
                        string res = String.Format("{0,-" + resNamesWidth + "}: {1," + resWidth + ":0.0} {2} ",
                            resource.Name, cell.GetStock(resource.Id), world.Units[resource.UnitId].Symbol);
                        int resPaddingLen = cellWidth - 2 - res.Length;
                        string resPadding = (resPaddingLen > 0) ? new String(' ', resPaddingLen) : "";
                        Console.Write(col + res + resPadding);
                    }

                    Console.WriteLine("\u2502");
                }
            }

            //-- Table Footer
            for (int x = 0; x < world.Map.SizeX; x++)
            {
                Char col = Corner(x, world.Map.SizeY, world.Map.SizeX, world.Map.SizeY);
                string cellFooter = new String('\u2500', cellWidth - 1);
                Console.Write(col + cellFooter);
            }

            Console.WriteLine(Corner(world.Map.SizeX, world.Map.SizeY, world.Map.SizeX, world.Map.SizeY));
        }

        private static Char Corner(int x, int y, int sizeX, int sizeY)
        {
            return Cross(y > 0, x < sizeX, y < sizeY, x > 0);
        }

        private static Char Cross(bool top, bool right, bool bottom, bool left)
        {
            int index = (top ? 1 : 0) + (right ? 2 : 0) + (bottom ? 4 : 0) + (left ? 8 : 0);
            Char[] crosses =
            {
                ' ', '\u2575', '\u2576', '\u2514', '\u2577', '\u2502', '\u250C', '\u251C', '\u2574', '\u2518', '\u2500',
                '\u2534', '\u2510', '\u2524', '\u252C', '\u253C'
            };
            return crosses[index];
        }

        private static Char Efficiency(ICell cell)
        {
            if (!cell.Jm2.IsNull())
            {
                return EfficiencyChar(cell.Jm2.Efficiency);
            }
            else
            {
                return EfficiencyChar(null);
            }
        }

        private static Char EfficiencyChar(float? efficiency)
        {
            if (efficiency == null)
            {
                return '\u2500';
            }

            if (efficiency >= 1.0f)
            {
                return '\u2588';
            }

            float eff = (float) efficiency;
            Int32 index = Convert.ToInt32((eff - Math.Truncate(eff)) * 8.0f);
            Char[] blocks =
            {
                ' ', '\u2581', '\u2582', '\u2583', '\u2584', '\u2585', '\u2586', '\u2587', '\u2588'
            };
            return blocks[index];
        }

        private static void PrintProgressBar(long min, long current, long max)
        {
            if (max > min)
            {
                double progress = Convert.ToDouble(current - min) / Convert.ToDouble(max - min);
                int p = Convert.ToInt32(Math.Floor(20.0d * progress));
                Console.Write("[");
                if (p <= 20)
                {
                    Console.Write(new String('\u2588', p));
                    Console.Write(new String(' ', 20 - p));
                }
                else
                {
                    Console.Write(new String('\u2588', 18) + ">>");
                }

                Console.Write("] {0,3}%", Convert.ToInt32(progress * 100.0d));
            }
        }

        private static void Test()
        {
            //Console.SetWindowSize(40, 40); 
            //Console.SetBufferSize(80, 80); 
            //Console.Clear();
            Console.WriteLine("Bla bla bla");

            (int, int) startPos = Console.GetCursorPosition();

            Console.Write("\u250F");
            for (int j = 0; j < 20; j++)
            {
                Console.Write("\u2501");
            }

            Console.Write("\u2513");
            Console.WriteLine();
            for (int i = 0; i < 5; i++)
            {
                Console.Write("\u2503");
                for (int j = 0; j < 20; j++)
                {
                    Console.Write(" ");
                }

                Console.Write("\u2503");
                Console.WriteLine();
            }

            Console.Write("\u2517");
            for (int j = 0; j < 20; j++)
            {
                Console.Write("\u2501");
            }

            Console.Write("\u251B");
            Console.WriteLine();
            Console.SetCursorPosition(startPos.Item1 + 2, startPos.Item2 + 1);
            Console.WriteLine("Hello GFG!");
        }
    }
}
