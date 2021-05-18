using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WorldSim.API;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WorldSim.IO
{
    public class UnitFileData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Symbol { get; set; }
    }

    public class ResourceFileData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string? Unit_Id { get; set; }
        public string? Distribution { get; set; }
        public int? Range { get; set; }
        public float? Attenuation { get; set; }
    }

    public class KpiFileData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Formula { get; set; }
        public string Unit_Id { get; set; }
    }

    public class TimeFileData
    {
        public string StepUnit { get; set; }
        public int StepValue { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class ScenarioData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string[] Targets  { get; set; }
    }

    public class MapFileData
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public CellFileData[] Cells { get; set; }
    }

    public class StockFileData : Dictionary<string, float>
    {
    }

    public class CellFileData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public StockFileData Stocks { get; set; }
        public string Jm2_Id { get; set; }
        public Dictionary<object, object> Jm2_Init { get; set; }
    }

    public class YamlFileData
    {
        //-- Header
        public string Format { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public DateTime ModDate { get; set; }
        public Dictionary<string, string> Author { get; set; }

        //-- Background
        public UnitFileData[] Units { get; set; }
        public ResourceFileData[] Resources { get; set; }
        public KpiFileData[] Kpis { get; set; }
        public TimeFileData Time { get; set; }
        public ScenarioData Scenario  { get; set; }
        public MapFileData Map { get; set; }

        //-- Current Time
        public DateTime CurrentTime { get; set; }
    }

    public class Importer
    {
        protected string FileName;
        protected IWorld World;

        public Importer(IWorld world, string fileName)
        {
            World = world;
            FileName = fileName;
        }

        public TimeSpan LoadDelay { get; set; }
        public TimeSpan CurrentStateDelay { get; set; }

        public DateTime LoadYaml(bool dontRun = false)
        {
            DateTime currentTime;
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var fileReader = new StreamReader(FileName))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new UnderscoredNamingConvention())
                    .Build();
                var fileData = deserializer.Deserialize<YamlFileData>(fileReader);
                currentTime = ProcessFileData(fileData, dontRun);
            }

            stopWatch.Stop();
            LoadDelay = stopWatch.Elapsed;
            return currentTime;
        }


        protected DateTime ProcessFileData(YamlFileData fileData, bool dontRun = false)
        {
            DateTime currentTime;
            //-- Header
            if (!string.IsNullOrWhiteSpace(fileData.Type)) World.Type = fileData.Type;

            if (!string.IsNullOrWhiteSpace(fileData.Version)) World.Version = fileData.Version;

            if (!fileData.ModDate.Equals(new DateTime(0))) World.ModDate = fileData.ModDate;

            World.Author = fileData.Author;

            //-- Background
            foreach (var u in fileData.Units)
            {
                if (u != null)
                {
                    if (string.IsNullOrWhiteSpace(u.Id)) throw new Exception("Unit must have an id");
                    World.Units.Add(u.Id, World.CreateUnit(u.Id, u.Name, u.Description, u.Symbol));
                }
            }

            foreach (var r in fileData.Resources)
            {
                if (string.IsNullOrWhiteSpace(r.Id)) throw new Exception("Resource must have an id");
                World.Resources.Add(r.Id,
                    World.CreateResource(r.Id, r.Name, r.Description, r.Type,
                        string.IsNullOrWhiteSpace(r.Unit_Id) ? null : World.Units[r.Unit_Id],
                        r.Distribution, r.Range, r.Attenuation));
            }

            foreach (var k in fileData.Kpis)
                World.Kpis.Add(World.CreateKpi(k.Name, k.Description, k.Formula,
                    !string.IsNullOrEmpty(k.Unit_Id) ? World.Units[k.Unit_Id] : null));

            World.Time.StepUnit = fileData.Time.StepUnit switch
            {
                "month" => TimeStep.month,
                "day" => TimeStep.day,
                _ => TimeStep.year
            };
            World.Time.StepValue = fileData.Time.StepValue;
            World.Time.Start = fileData.Time.Start;
            World.Time.End = fileData.Time.End;

            World.CreateMap(fileData.Map.SizeX, fileData.Map.SizeY);
            foreach (var cell in fileData.Map.Cells)
            {
                if (cell.Stocks != null)
                    foreach (var stock in cell.Stocks)
                        World.Map.Cells[cell.X, cell.Y].SetInitialStock(stock.Key, stock.Value);

                if (!string.IsNullOrWhiteSpace(cell.Jm2_Id))
                {
                    var jm2 = World.CreateJM2(cell.Jm2_Id, DataDictionary.ConvertGenericData(cell.Jm2_Init));
                    World.Map.Cells[cell.X, cell.Y].Jm2 = jm2;
                }
            }

            //-- Current Simulation State
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            currentTime = World.Time.Current;
            if (fileData.CurrentTime.Ticks != DateTime.MinValue.Ticks)
            {
                currentTime = fileData.CurrentTime;
                if (!dontRun) World.Time.Current = fileData.CurrentTime;
            }

            stopWatch.Stop();
            CurrentStateDelay = stopWatch.Elapsed;
            //-- Done
            return currentTime;
        }
    }
}
