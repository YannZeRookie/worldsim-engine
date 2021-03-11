using System;
using System.Collections.Generic;
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

        // KPI Factory
        public IKpi CreateKpi(string name, string description, string formula, IUnit? unit)
        {
            return new Kpi(name, description, formula, unit);
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
                case "source":
                    return new JM2Source(init);
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
                ((Cell) cell).StepExecute(currentTime, annualDivider);
            }

            //-- Finalization
            foreach (var cell in this.Map.Cells)
            {
                ((Cell) cell).StepFinalize(currentTime);
            }
        }
    }
}