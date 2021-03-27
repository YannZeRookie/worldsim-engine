using System;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Engine
{
    public class Map : IMap
    {
        public Int32 SizeX { get; }
        public Int32 SizeY { get; }
        public ICell[,] Cells { get; set; }

        public Map(Int32 sizeX, Int32 sizeY)
        {
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            this.Cells = new Cell[sizeX, sizeY];
        }

        public void Init(IDictionary<string, IResource> resources)
        {
            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    this.Cells[x, y] = new Cell(x, y, resources);
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
        
        public float TotalDemand(string resourceId)
        {
            float total = 0.0f;
            foreach (var c in Cells)
            {
                Cell cell = (Cell) c;
                float demand = cell.GetDemandFor(resourceId);
                if (demand > 0.0f)
                {
                    total += demand;
                }
            }

            return total;
        }

        public float TotalStock(string resourceId)
        {
            float total = 0.0f;
            foreach (var c in Cells)
            {
                total += c.GetStock(resourceId);
            }
            return total;
        }
    }
}