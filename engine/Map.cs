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
}