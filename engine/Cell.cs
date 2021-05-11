using System;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Model
{
    public class Cell : ICell
    {
        /// <summary>
        ///     Resource demand for the current Iteration
        /// </summary>
        private readonly IDictionary<string, float> _demand;

        private readonly IDictionary<string, float> _output;

        public Cell(int x, int y, IDictionary<string, IResource> resources)
        {
            X = x;
            Y = y;
            Resources = resources;
            InitialStocks = new Dictionary<string, float>();
            Stocks = new Dictionary<string, float>();
            foreach (var r in resources) Stocks[r.Key] = 0.0f;

            _output = new Dictionary<string, float>();
            _demand = new Dictionary<string, float>();
        }

        private IDictionary<string, IResource> Resources { get; }
        public IDictionary<string, float> Stocks { get; set; }
        private IDictionary<string, float> InitialStocks { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public IJM2 Jm2 { get; set; }

        public float GetStock(string resourceId)
        {
            return Stocks[resourceId];
        }

        public void SetStock(string resourceId, float stock)
        {
            Stocks[resourceId] = stock;
        }

        public float GetInitialStock(string resourceId)
        {
            return InitialStocks[resourceId];
        }

        public void SetInitialStock(string resourceId, float stock)
        {
            InitialStocks[resourceId] = stock;
        }

        public override string ToString()
        {
            var start = string.Format("Cell[{0},{1}]: ", X, Y);
            var result = "";
            foreach (var r in Resources) result += r.Value.ValueToString(Stocks[r.Key]) + " ";

            if (Jm2 != null && Jm2.Efficiency != null)
                result += string.Format(" Efficiency: {0,3:0}%", Jm2.Efficiency * 100.0f);

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

        public string Id()
        {
            return X + "-" + Y;
        }

        public int DistanceTo(Cell cell)
        {
            return Math.Max(Math.Abs(cell.X - X), Math.Abs(cell.Y - Y));
        }

        public void Restart()
        {
            foreach (var r in Resources) Stocks[r.Key] = InitialStocks.ContainsKey(r.Key) ? InitialStocks[r.Key] : 0.0f;

            ((JM2) Jm2)?.Restart();
        }

        public float GetDemandFor(string resourceId)
        {
            if (_demand.ContainsKey(resourceId))
                return _demand[resourceId];
            return 0.0f;
        }

        public void StepPrepare(Time currentTime)
        {
            _output.Clear();
            _demand.Clear();
            ((JM2) Jm2)?.DescribeDemand(currentTime, _demand);
        }

        public void StepExecute(Map map, Time currentTime, Allocator allocator)
        {
            ((JM2) Jm2)?.Step(Stocks, currentTime, allocator, this, _output);
        }

        public void StepFinalize(Time currentTime)
        {
            foreach (var o in _output)
                if (Resources[o.Key].Type == "volatile")
                    Stocks[o.Key] = o.Value;
                else
                    Stocks[o.Key] += o.Value;
        }
    }
}
