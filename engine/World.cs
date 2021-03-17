using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            this.Kpis = new List<IKpi>();
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
        public List<IKpi> Kpis { get; set; }
        public ITime Time { get; set; }

        public IMap Map
        {
            get => _map;
        }

        //-- Background utilities
        /// <summary>
        /// Compute the maximum text width of all KPIs
        /// </summary>
        /// <returns></returns>
        public int GetKpisMaxWidth()
        {
            int maxWidth = 0;
            foreach (var kpi in Kpis)
            {
                maxWidth = Math.Max(maxWidth, kpi.Name.Length);
            }

            return maxWidth;
        }

        /// <summary>
        /// Compute the various widths and other parameters that will be used for display formating
        /// </summary>
        /// <returns>List of various dimensions</returns>
        public IDictionary<string, int> ComputeWidths()
        {
            int resNamesWidth = 0;
            int unitNamesWidth = 0;
            int resWidth = 8;
            foreach (var resource in Resources.Values)
            {
                if (resource.Name.Length > resNamesWidth) resNamesWidth = resource.Name.Length;
                if (resource.Unit != null && resource.Unit.Symbol.Length > unitNamesWidth)
                    unitNamesWidth = resource.Unit.Symbol.Length;
            }

            int cellExtraLines = 0;
            int cellExtraWidth = 0;
            foreach (ICell cell in _map.Cells)
            {
                int extraLines = cell.NbExtraLines();
                if (extraLines > cellExtraLines) cellExtraLines = extraLines;
                int extraWidth = cell.ExtraWidth();
                if (extraWidth > cellExtraWidth) cellExtraWidth = extraWidth;
            }

            return new Dictionary<string, int>()
            {
                {"resNames", resNamesWidth}, // Maximum width of Resource names
                {"unitNames", unitNamesWidth}, // Maximum width of Resource units
                {"res", resWidth}, // Maximum width of Resource values
                {"cellWidth", Math.Max(1 + resNamesWidth + 2 + resWidth + 1 + unitNamesWidth + 2, cellExtraWidth)},
                {"cellExtraLines", cellExtraLines},
                {"kpiMaxWidth", GetKpisMaxWidth()}
            };
        }


        //-- Factories & Tools
        // Unit Factory
        public IUnit CreateUnit(string id, string name, string description, string symbol)
        {
            return new Unit(id, name, description, symbol);
        }

        // Resource Factory
        public IResource CreateResource(string id, string name, string description, string type, IUnit? unit)
        {
            return new Resource(id, name, description, type, unit);
        }

        // KPI Factory
        public IKpi CreateKpi(string name, string description, string formula, IUnit? unit)
        {
            //-- Resource Sum
            MatchCollection matches = new Regex(@"^sum\((\w+)\)$").Matches(formula);
            if (matches.Count == 1)
            {
                string resourceId = matches.First().Groups[1].Value;
                return new KpiResourceSum(name, description, formula, unit, resourceId);
            }
            
            //-- Iteration number
            if (formula == "iteration")
            {
                return new KpiIteration(name, description, formula, unit);
            }
            

            throw new Exception("Error: could not understand formula: " + formula);
        }

        // Map Factory
        public void CreateMap(Int32 sizeX, Int32 sizeY)
        {
            Map map = new Map(sizeX, sizeY);
            map.Init(this.Resources);
            this._map = map;
        }

        // JM2 Factory
        public IJM2 CreateJM2(string jm2Id, IDictionary<string, object> init)
        {
            switch (jm2Id)
            {
                case "source":
                    return new JM2Source(init);
                case "sourceMinMax":
                    return new Jm2SourceMinMax(init);
                case "sink":
                    return new JM2Sink(init);
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

        public void Step(Time currentTime)
        {
            //-- Run all JM2s on all Cells
            //-- Preparation
            foreach (var cell in this.Map.Cells)
            {
                ((Cell) cell).StepPrepare(currentTime);
            }

            //-- Execution
            float annualDivider = this.Time.GetAnnualDivider();
            foreach (var cell in this.Map.Cells)
            {
                ((Cell) cell).StepExecute((Map) this.Map, currentTime, annualDivider);
            }

            //-- Finalization
            foreach (var cell in this.Map.Cells)
            {
                ((Cell) cell).StepFinalize(currentTime);
            }
        }
    }
}
