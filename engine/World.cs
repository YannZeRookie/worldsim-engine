using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChoETL;
using Newtonsoft.Json.Linq;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class World : IWorld
    {
        private Map _map;

        public World()
        {
            this.Units = new Dictionary<string, IUnit>();
            this.Resources = new Dictionary<string, IResource>();
            this.KeyAttributes = new List<IKeyAttributes>();
            this.Time = new Time(this);
        }

        //-- Metadata
        public string Type { get; set; }
        public string Version { get; set; }
        public DateTime ModDate { get; set; }

        public Dictionary<string, string> Author { get; set; }

        //-- Background
        public Dictionary<string, IUnit> Units { get; set; }
        public Dictionary<string, IResource> Resources { get; set; }
        public List<IKeyAttributes> KeyAttributes { get; set; }
        public ITime Time { get; set; }

        public IMap Map
        {
            get => _map;
        }

        //-- Factories & Tools
        // Unit Factory
        public IUnit CreateUnit(string id, string name, string description, string symbol)
        {
            return new Unit(id, name, description, symbol);
        }

        // Resource Factory
        public IResource CreateResource(string id, string name, string description, string type, string unitId)
        {
            return new Resource(id, name, description, type, unitId);
        }

        // KeyAttribute Factory
        public IKeyAttributes CreateKeyAttribute(string name, string description, string formula, string unitId)
        {
            return new KeyAttributes(name, description, formula, unitId, this);
        }

        // Map Factory
        public void CreateMap(Int32 sizeX, Int32 sizeY)
        {
            Map map = new Map(sizeX, sizeY);
            map.Init(this.Resources, this);
            this._map = map;
        }

        // JM2 Factory
        public IJM2 CreateJM2(string jm2Id, IDictionary<string, object> init)
        {
            switch (jm2Id)
            {
                case "mine":
                    return new JM2Mine(init);
                case "factory":
                    return new JM2Factory(init);
                default:
                    throw new Exception("Unknown JM2 of id=" + jm2Id);
            }
        }

        //-- Running time
        public void Restart()
        {
            _map.Restart();
        }

        public void Step()
        {
            //-- Run all JM2s on all Cells
            //-- Preparation
            foreach (var cell in this.Map.Cells)
            {
                ((Cell) cell).StepPrepare();
            }

            //-- Execution
            float annualDivider = this.Time.GetAnnualDivider();
            foreach (var cell in this.Map.Cells)
            {
                ((Cell) cell).StepExecute(annualDivider);
            }

            //-- Finalization
            foreach (var cell in this.Map.Cells)
            {
                ((Cell) cell).StepFinalize();
            }
        }
    }

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

    public class KeyAttributes : IKeyAttributes
    {
        private World _world;
        private string _resourceID;
        public string Name { get; set; }
        public string Description { get; set; }
        public string Formula { get; set; }
        public string UnitId { get; set; }

        public KeyAttributes(string name, string description, string formula, string unitId, IWorld world)
        {
            Name = name;
            Description = description;
            Formula = formula;
            UnitId = unitId;
            _world = (World) world;
            Regex rx = new Regex(@"^sum\((\w+)\)$");
            MatchCollection matches = rx.Matches(formula);
            if (matches.Count == 1)
            {
                _resourceID = matches.First().Groups[1].Value;
            }
            else
            {
                throw new Exception("Error: could not understand formula: " + formula);
            }
        }

        public float GetValue()
        {
            float result = 0.0f;
            foreach (var cell in _world.Map.Cells)
            {
                result += cell.GetStock(_resourceID);
            }

            return result;
        }

        public override string ToString()
        {
            float value = GetValue();
            string symbol = _world.Units[UnitId].Symbol;
            return String.Format("{0}={1,8:0.0} {2}", Name, value, symbol);
        }
    }

    public class Time : ITime
    {
        private World _world;
        private DateTime _current;
        private Int32 _iteration;

        public Time(World world)
        {
            this.StepUnit = TimeStep.year;
            this.StepValue = 1;
            this.Start = new DateTime(1800, 1, 1);
            this.End = new DateTime(2101, 1, 1);
            _world = world;
            _current = this.Start;
            _iteration = 0;
        }

        public TimeStep StepUnit { get; set; }
        public Int32 StepValue { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public DateTime Current
        {
            get => _current;
            set { RunTo(value); }
        }

        public Int32 Iteration
        {
            get => _iteration;
            set { IterateTo(value); }
        }

        public float GetAnnualDivider()
        {
            switch (StepUnit)
            {
                case TimeStep.year:
                    return 1.0f / StepValue;
                case TimeStep.month:
                    return 12.0f / StepValue;
                case TimeStep.day:
                    if (DateTime.IsLeapYear(Current.Year))
                    {
                        return 365.0f / StepValue;
                    }
                    else
                    {
                        return 365.0f / StepValue;
                    }
            }

            return 1.0f;
        }

        public void Restart()
        {
            _current = this.Start;
            _iteration = 0;
            _world.Restart();
        }

        public void Step()
        {
            switch (this.StepUnit)
            {
                case TimeStep.year:
                    _current = _current.AddYears(this.StepValue);
                    break;
                case TimeStep.month:
                    _current = _current.AddMonths(this.StepValue);
                    break;
                case TimeStep.day:
                    _current = _current.AddDays(this.StepValue);
                    break;
            }

            _iteration++;
            _world.Step();
        }

        public void RunTo(DateTime targetDate)
        {
            if (targetDate < _current)
            {
                // We need to go back to square one before fast-forwarding
                Restart();
            }

            // Now just fast forward
            while (targetDate > _current)
            {
                Step();
            }
        }

        public void IterateTo(Int32 iteration)
        {
            iteration = Math.Max(0, iteration);
            if (iteration < _iteration)
            {
                // We need to go back to square one before fast-forwarding
                Restart();
            }

            // Now just fast forward
            while (iteration > _iteration)
            {
                Step();
            }
        }
    }

    public class Map : IMap
    {
        public Map(Int32 sizeX, Int32 sizeY)
        {
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            this.Cells = new Cell[sizeX, sizeY];
        }

        public void Init(Dictionary<string, IResource> resources, World world)
        {
            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    this.Cells[x, y] = new Cell(x, y, resources, world);
                }
            }
        }

        public void Restart()
        {
            foreach (var cell in Cells)
            {
                ((Cell) cell).Restart();
            }
        }

        public Int32 SizeX { get; }
        public Int32 SizeY { get; }
        public ICell[,] Cells { get; set; }

        public float FindResource(string resourceId, float needs)
        {
            float found = 0.0f;
            foreach (var cell in Cells)
            {
                float stock = cell.GetStock(resourceId);
                float used = Math.Min(needs, stock);
                cell.SetStock(resourceId, stock - used);
                found += used;
                needs -= used;
            }

            return found;
        }
    }

    public class Cell : ICell
    {
        private World _world;
        private Dictionary<string, float> _output;
        public Dictionary<string, float> Stocks { get; set; }

        public Cell(Int32 x, Int32 y, Dictionary<string, IResource> resources, IWorld world)
        {
            this.X = x;
            this.Y = y;
            this.Stocks = new Dictionary<string, float>();
            foreach (var r in resources)
            {
                this.Stocks[r.Key] = 0.0f;
            }

            _output = new Dictionary<string, float>();
            _world = (World) world;
        }

        public void Restart()
        {
            foreach (var k in this.Stocks.Keys)
            {
                this.Stocks[k] = 0.0f;
            }

            ((JM2) Jm2)?.Restart();
        }

        public Int32 X { get; set; }
        public Int32 Y { get; set; }
        public IJM2 Jm2 { get; set; }

        public float GetStock(string resourceId)
        {
            return this.Stocks[resourceId];
        }

        public void SetStock(string resourceId, float stock)
        {
            this.Stocks[resourceId] = stock;
        }

        public override string ToString()
        {
            string start = String.Format("Cell[{0},{1}]: ", X, Y);
            string result = "";
            foreach (var s in Stocks)
            {
                string unitId = _world.Resources[s.Key].UnitId;
                string symbol = _world.Units[unitId].Symbol;
                result += String.Format("{0}={1,10:0.0} {2} ", s.Key, s.Value, symbol);
            }

            if (!this.Jm2.IsNull() && !this.Jm2.Efficiency.IsNull())
            {
                result += String.Format(" Efficiency= {0,3:0}%", this.Jm2.Efficiency * 100.0f);
            }

            return start + result;
        }

        public void StepPrepare()
        {
            _output.Clear();
        }

        public void StepExecute(float annualDivider)
        {
            JM2 jm2 = (JM2) this.Jm2;
            if (jm2 != null)
            {
                jm2.Step((Map) _world.Map, this.Stocks, annualDivider, _output);
            }
        }

        public void StepFinalize()
        {
            foreach (var o in _output)
            {
                this.Stocks[o.Key] += o.Value;
            }
        }
    }

    public abstract class JM2 : IJM2
    {
        public string Id { get; set; }
        public float? Efficiency { get; set; }

        public JM2()
        {
            Efficiency = null;
        }

        public virtual void Restart()
        {
        }

        public virtual void Step(Map map, Dictionary<string, float> stocks, float annualDivider,
            Dictionary<string, float> output)
        {
        }

        public float ConsumeResource(string resourceId, float needs, Map map, Dictionary<string, float> stocks)
        {
            //-- Try local stock first
            float found = Math.Min(stocks[resourceId], needs);
            stocks[resourceId] -= found;
            float notFound = needs - found;
            if (notFound > 0)
            {
                float foundElsewhere = map.FindResource(resourceId, notFound);
                found += foundElsewhere;
            }

            return found;
        }
    }

    public class JM2Mine : JM2
    {
        private string _resourceId;
        private float _reserve;
        private float _production;
        private IDictionary<string, object> _init;

        public JM2Mine(IDictionary<string, object> init)
        {
            Id = "mine";
            _init = init;
            Restart();
        }

        public override void Restart()
        {
            _resourceId = _init["resource_id"] as string;
            _reserve = Convert.ToSingle(_init["reserve"]);
            _production = Convert.ToSingle(_init["production"]);
        }

        public override void Step(Map map, Dictionary<string, float> stocks, float annualDivider,
            Dictionary<string, float> output)
        {
            float productionTarget = _production / annualDivider;
            float mined = Math.Min(_reserve, productionTarget);
            output[_resourceId] = mined;
            Efficiency = (mined < _reserve) ? 1.0f : (_reserve > 0.0f ? mined / productionTarget : 0.0f);
            _reserve -= mined;
        }
    }

    public class JM2Factory : JM2
    {
        private Dictionary<string, float> _opex;
        private Dictionary<string, float> _output;
        private IDictionary<string, object> _init;

        public JM2Factory(IDictionary<string, object> init)
        {
            Id = "factory";
            _init = init;
            _opex = new Dictionary<string, float>();
            _output = new Dictionary<string, float>();
            Restart();
        }

        public override void Restart()
        {
            _opex.Clear();
            foreach (JObject o in (IEnumerable) _init["opex"])
            {
                string resourceId = (string) ((JValue) o["resource_id"]).Value;
                _opex[resourceId] = Convert.ToSingle(((JValue) o["consumption"]).Value);
            }

            _output.Clear();
            foreach (JObject o in (IEnumerable) _init["output"])
            {
                string resource_id = (string) ((JValue) o["resource_id"]).Value;
                _output[resource_id] = Convert.ToSingle(((JValue) o["production"]).Value);
            }
        }

        public override void Step(Map map, Dictionary<string, float> stocks, float annualDivider,
            Dictionary<string, float> output)
        {
            float efficiency = 1.0f;
            foreach (var supply in _opex)
            {
                float needs = supply.Value / annualDivider;
                float consumed = ConsumeResource(supply.Key, needs, map, stocks);
                if (needs > 0.0f)
                    efficiency = Math.Min(efficiency, consumed / needs); // We are only as strong as our weakest point
            }

            Efficiency = efficiency;

            foreach (var production in _output)
            {
                output[production.Key] = production.Value * efficiency / annualDivider;
            }
        }
    }
}