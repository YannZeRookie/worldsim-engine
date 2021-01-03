using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChoETL;
using WorldSim.API;

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
        public string Unit_Id { get; set; }
    }

    public class KeyAttributeFileData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Formula { get; set; }
        public string Unit_Id { get; set; }
    }

    public class TimeFileData
    {
        public string StepUnit { get; set; }
        public Int32 StepValue { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class MapFileData
    {
        public Int32 SizeX { get; set; }
        public Int32 SizeY { get; set; }
        public CellFileData[] Cells { get; set; }
    }

    public class CellFileData
    {
        public Int32 X { get; set; }
        public Int32 Y { get; set; }
        public string Jm2_Id { get; set; }
        public Dictionary<string, object> Jm2_Init { get; set; }
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
        public KeyAttributeFileData[] KeyAttributes { get; set; }
        public TimeFileData Time { get; set; }
        public MapFileData Map { get; set; }

        //-- Current Time
        public DateTime CurrentTime { get; set; }
    }

    public class Importer
    {
        protected IWorld World;
        protected string FileName;
        public TimeSpan LoadDelay { get; set; }
        public TimeSpan CurrentStateDelay { get; set; }

        public Importer(IWorld world, string fileName)
        {
            this.World = world;
            this.FileName = fileName;
        }

        public DateTime LoadYaml(bool dontRun = false)
        {
            DateTime currentTime;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            using (var r = new ChoYamlReader<YamlFileData>(this.FileName))
            {
                YamlFileData fileData = r.First();
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
            if (!String.IsNullOrWhiteSpace(fileData.Type))
            {
                this.World.Type = fileData.Type;
            }

            if (!String.IsNullOrWhiteSpace(fileData.Version))
            {
                this.World.Version = fileData.Version;
            }

            if (!fileData.ModDate.Equals(new DateTime(0)))
            {
                this.World.ModDate = fileData.ModDate;
            }

            this.World.Author = fileData.Author;
            //-- Background
            foreach (var u in fileData.Units)
            {
                if (string.IsNullOrWhiteSpace(u.Id)) throw new Exception("Unit must had an id");
                this.World.Units.Add(u.Id, this.World.CreateUnit(u.Id, u.Name, u.Description, u.Symbol));
            }

            foreach (var r in fileData.Resources)
            {
                if (string.IsNullOrWhiteSpace(r.Id)) throw new Exception("Resource must had an id");
                this.World.Resources.Add(r.Id,
                    this.World.CreateResource(r.Id, r.Name, r.Description, r.Type, r.Unit_Id));
            }

            foreach (var k in fileData.KeyAttributes)
            {
                this.World.KeyAttributes.Add(this.World.CreateKeyAttribute(k.Name, k.Description, k.Formula,
                    k.Unit_Id));
            }

            this.World.Time.StepUnit = fileData.Time.StepUnit switch
            {
                "month" => TimeStep.month,
                "day" => TimeStep.day,
                _ => TimeStep.year
            };
            this.World.Time.StepValue = fileData.Time.StepValue;
            this.World.Time.Start = fileData.Time.Start;
            this.World.Time.End = fileData.Time.End;

            this.World.CreateMap(fileData.Map.SizeX, fileData.Map.SizeY);
            foreach (var cell in fileData.Map.Cells)
            {
                if (!string.IsNullOrWhiteSpace(cell.Jm2_Id))
                {
                    IJM2 jm2 = this.World.CreateJM2(cell.Jm2_Id, cell.Jm2_Init);
                    this.World.Map.Cells[cell.X, cell.Y].Jm2 = jm2;
                }
            }

            //-- Current Simulation State
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            currentTime = this.World.Time.Current;
            if (fileData.CurrentTime.Ticks != DateTime.MinValue.Ticks)
            {
                currentTime = fileData.CurrentTime;
                if (!dontRun)
                {
                    this.World.Time.Current = fileData.CurrentTime;
                }
            }
            stopWatch.Stop();
            CurrentStateDelay = stopWatch.Elapsed;
            //-- Done
            return currentTime;
        }
    }
}