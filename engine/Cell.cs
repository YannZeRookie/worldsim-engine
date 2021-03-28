using System;
using System.Collections.Generic;
using ChoETL;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class Cell : ICell
    {
        private IDictionary<string, float> _output;

        /// <summary>
        /// Resource demand for the current Iteration
        /// </summary>
        private IDictionary<string, float> _demand;

        /// <summary>
        /// Resources allocations for the current Iteration
        /// </summary>
        private IDictionary<string, Allocation> _allocations;

        private IDictionary<string, IResource> Resources { get; set; }
        public IDictionary<string, float> Stocks { get; set; }
        private IDictionary<string, float> InitialStocks { get; }
        public Int32 X { get; set; }
        public Int32 Y { get; set; }
        public IJM2 Jm2 { get; set; }

        public Cell(Int32 x, Int32 y, IDictionary<string, IResource> resources)
        {
            this.X = x;
            this.Y = y;
            this.Resources = resources;
            this.InitialStocks = new Dictionary<string, float>();
            this.Stocks = new Dictionary<string, float>();
            foreach (var r in resources)
            {
                this.Stocks[r.Key] = 0.0f;
            }

            _output = new Dictionary<string, float>();
            _demand = new Dictionary<string, float>();
            _allocations = new Dictionary<string, Allocation>();
        }

        public string Id()
        {
            return X + "-" + Y;
        }

        public void Restart()
        {
            foreach (var r in Resources)
            {
                this.Stocks[r.Key] = InitialStocks.ContainsKey(r.Key) ? InitialStocks[r.Key] : 0.0f;
            }

            ((JM2) Jm2)?.Restart();
        }

        public float GetStock(string resourceId)
        {
            return this.Stocks[resourceId];
        }

        public void SetStock(string resourceId, float stock)
        {
            this.Stocks[resourceId] = stock;
        }

        public float GetInitialStock(string resourceId)
        {
            return this.InitialStocks[resourceId];
        }

        public void SetInitialStock(string resourceId, float stock)
        {
            this.InitialStocks[resourceId] = stock;
        }

        public float GetDemandFor(string resourceId)
        {
            if (_demand.ContainsKey(resourceId))
                return _demand[resourceId];
            return 0.0f;
        }

        public override string ToString()
        {
            string start = String.Format("Cell[{0},{1}]: ", X, Y);
            string result = "";
            foreach (KeyValuePair<string, IResource> r in Resources)
            {
                result += r.Value.ValueToString(Stocks[r.Key]) + " ";
            }

            if (!this.Jm2.IsNull() && !this.Jm2.Efficiency.IsNull())
            {
                result += String.Format(" Efficiency: {0,3:0}%", this.Jm2.Efficiency * 100.0f);
            }

            return start + result;
        }

        public string GetExtraLine(int extraLine)
        {
            return Jm2?.GetExtraLine(extraLine) ?? "";
        }

        public int NbExtraLines()
        {
            return Jm2?.NbExtraLines() ?? 0;
        }

        public int ExtraWidth()
        {
            return Jm2?.ExtraWidth() ?? 0;
        }

        public void StepPrepare(Time currentTime)
        {
            _output.Clear();
            _demand.Clear();
            _allocations.Clear();
            ((JM2) Jm2)?.DescribeDemand(currentTime, _demand);
        }

        public void StepExecute(Map map, Time currentTime)
        {
            ((JM2) Jm2)?.Step(this.Stocks, currentTime, _allocations, _output);
        }

        public void StepFinalize(Time currentTime)
        {
            foreach (var o in _output)
            {
                if (Resources[o.Key].Type == "volatile")
                    Stocks[o.Key] = o.Value;
                else
                    Stocks[o.Key] += o.Value;
            }
        }

        public void AddAllocation(string resourceId, Allocation allocation)
        {
            _allocations[resourceId] = allocation;
        }
    }
}
