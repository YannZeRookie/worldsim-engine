using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WorldSim.API;

namespace WorldSim.Model
{
    public class World : IWorld
    {
        private Map _map;

        public World()
        {
            Units = new Dictionary<string, IUnit>();
            Resources = new Dictionary<string, IResource>();
            Kpis = new List<IKpi>();
            Time = new Time(this);
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

        public IMap Map => _map;

        /// <summary>
        ///     Compute the various widths and other parameters that will be used for display formating
        /// </summary>
        /// <returns>List of various dimensions</returns>
        public IDictionary<string, int> ComputeWidths()
        {
            var resNamesWidth = 0;
            var unitNamesWidth = 0;
            var resWidth = 8;
            foreach (var resource in Resources.Values)
            {
                if (resource.Name.Length > resNamesWidth) resNamesWidth = resource.Name.Length;
                if (resource.Unit != null && resource.Unit.Symbol.Length > unitNamesWidth)
                    unitNamesWidth = resource.Unit.Symbol.Length;
            }

            var cellExtraLines = 0;
            var cellExtraWidth = 0;
            foreach (var cell in _map.Cells)
            {
                var extraLines = cell.NbExtraLines();
                if (extraLines > cellExtraLines) cellExtraLines = extraLines;
                var extraWidth = cell.ExtraWidth();
                if (extraWidth > cellExtraWidth) cellExtraWidth = extraWidth;
            }

            return new Dictionary<string, int>
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
        public IResource CreateResource(string id, string name, string description, string type, IUnit? unit,
            string? distribution, int? range, float? attenuation)
        {
            return new Resource(id, name, description, type, unit, distribution, range, attenuation);
        }

        // KPI Factory
        public IKpi CreateKpi(string name, string description, string formula, IUnit? unit)
        {
            //-- Resource Sum
            var matches = new Regex(@"^sum\((\w+)\)$").Matches(formula);
            if (matches.Count == 1)
            {
                var resourceId = matches[0].Groups[1].Value;
                return new KpiResourceSum(this, name, description, formula, unit, resourceId);
            }

            //-- Iteration number
            if (formula == "iteration") return new KpiIteration(this, name, description, formula, unit);

            throw new Exception("Error: could not understand formula: " + formula);
        }

        // Map Factory
        public void CreateMap(int sizeX, int sizeY)
        {
            var map = new Map(sizeX, sizeY);
            map.Init(Resources);
            _map = map;
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

        //-- Background utilities
        /// <summary>
        ///     Compute the maximum text width of all KPIs
        /// </summary>
        /// <returns></returns>
        public int GetKpisMaxWidth()
        {
            var maxWidth = 0;
            foreach (var kpi in Kpis) maxWidth = Math.Max(maxWidth, kpi.Name.Length);

            return maxWidth;
        }

        //-- Running time
        public void Restart()
        {
            _map.Restart();
        }

        public void Step(Time currentTime)
        {
            //-- Run all JM2s on all Cells
            //-- Preparation: each cell will initialize itself and set-up its demand
            foreach (var cell in Map.Cells) ((Cell) cell).StepPrepare(currentTime);

            // Review all demands and allocate resources to each cell
            var allocator = Allocator.Allocate(currentTime, Resources, (Map) Map);

            //-- Execution: each cell with produce and/or consume stocks
            foreach (var cell in Map.Cells) ((Cell) cell).StepExecute((Map) Map, currentTime, allocator);

            //-- Finalization: its cell will update its stocks with productions and perform any needed clean-up tasks
            foreach (var cell in Map.Cells) ((Cell) cell).StepFinalize(currentTime);
        }
    }
}
