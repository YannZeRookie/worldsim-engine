using System;
using System.Collections.Generic;

namespace WorldSim
{
    namespace API
    {
        public interface IWorld
        {
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
            public IMap Map { get; }

            //-- Factories & Tools
            public IUnit CreateUnit(string id, string name, string description, string symbol);
            public IResource CreateResource(string id, string name, string description, string type, string unitId);
            public IKeyAttributes CreateKeyAttribute(string name, string description, string formula, string unitId);
            public void CreateMap(Int32 sizeX, Int32 sizeY);
            public IJM2 CreateJM2(string jm2Id, IDictionary<string, object> init);
        }

        public interface IUnit
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Symbol { get; set; }
        }

        public interface IResource
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string UnitId { get; set; }
        }

        public interface IKeyAttributes
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Formula { get; set; }
            public string UnitId { get; set; }
            public float GetValue(); // Get the value of this Key Attribute at the current time
            public string ToString(int padding);
        }

        public enum TimeStep
        {
            year,
            month,
            day
        }

        public interface ITime
        {
            public TimeStep StepUnit { get; set; }
            public Int32 StepValue { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public DateTime Current { get; set; }
            public Int32 Iteration { get; set; }
            public float GetAnnualDivider();
            public void Restart();
            public void Step();
            public void StepBack();
            public bool Reached(DateTime to)
            {
                return Current >= to;
            }
            public bool ReachedIteration(Int32 to)
            {
                return Iteration >= to;
            }
            public bool Done()
            {
                return Reached(End);
            }
        }

        public interface IMap
        {
            public Int32 SizeX { get; }
            public Int32 SizeY { get; }
            public ICell[,] Cells { get; set; }
        }

        public interface ICell
        {
            public Int32 X { get; set; }
            public Int32 Y { get; set; }
            public IJM2 Jm2 { get; set; }
            public float GetStock(string resourceId);
            public void SetStock(string resourceId, float stock);
            public string ToString();
        }

        public interface IJM2
        {
            public string Id { get; set; }
            public float? Efficiency { get;  }
        }
    }
}