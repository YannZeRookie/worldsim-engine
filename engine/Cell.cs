using System;
using System.Collections.Generic;
using ChoETL;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class Cell : ICell
    {
        private IDictionary<string, float> _output;
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
        }

        public void Restart()
        {
            foreach (var k in this.Stocks.Keys)
            {
                this.Stocks[k] = InitialStocks.ContainsKey(k) ? InitialStocks[k] : 0.0f;
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

        public override string ToString()
        {
            string start = String.Format("Cell[{0},{1}]: ", X, Y);
            string result = "";
            foreach (KeyValuePair<string,IResource> r in Resources)
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
        }

        public void StepExecute(Map map, Time currentTime, float annualDivider)
        {
            JM2 jm2 = (JM2) this.Jm2;
            if (jm2 != null)
            {
                jm2.Step(map, this.Stocks, currentTime, annualDivider, _output);
            }
        }

        public void StepFinalize(Time currentTime)
        {
            foreach (var o in _output)
            {
                this.Stocks[o.Key] += o.Value;
            }
        }
    }
}