using System;
using System.Collections;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Model
{
    public class Allocator
    {
        IDictionary<string, Allocation> Allocations { get; }

        public static Allocator Allocate(Time currentTime, Dictionary<string, IResource> resources, Map map)
        {
            Allocator allocator = new Allocator();
            allocator.AllocateDemand(currentTime, resources, map);
            return allocator;
        }

        public Allocator()
        {
            Allocations = new Dictionary<string, Allocation>();
        }

        /// <summary>
        /// Allocate the resources to try to satisfy demand as well as possible
        /// </summary>
        /// <param name="currentTime"></param>
        private void AllocateDemand(Time currentTime, Dictionary<string, IResource> resources, Map map)
        {
            foreach (var resource in resources)
            {
                AllocateResourceToDemand(currentTime, (Resource) resource.Value, map);
            }
        }

        /// <summary>
        /// Allocate the resource using a Spread distribution,
        /// i.e. all stocks are spread evenly across all demands
        /// across the map on a pro-rata basis of the demands.
        /// If a cell is asking for X % of the total demand,
        /// it will allocated with X % of what's available.
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="resource"></param>
        /// <param name="map"></param>
        private void AllocateResourceToDemand(Time currentTime, Resource resource, Map map)
        {
            //-- Build the list of Stocks and Demands
            List<IDictionary<string, float>> stocks = new List<IDictionary<string, float>>();
            List<Cell> stockCells = new List<Cell>();
            List<Cell> demands = new List<Cell>();
            foreach (var c in map.Cells)
            {
                Cell cell = (Cell) c;
                if (cell.GetStock(resource.Id) > 0.0f)
                {
                    stockCells.Add(cell);
                    stocks.Add(cell.Stocks);
                }

                if (cell.GetDemandFor(resource.Id) > 0.0f)
                {
                    demands.Add(cell);
                }
            }

            //-- Step 1: build the Clusters table
            //   (for now everyone can use everyone)
            float[,] cluster = new float[demands.Count, stocks.Count];
            for (int i = 0; i < demands.Count; i++)
            {
                for (int j = 0; j < stocks.Count; j++)
                {
                    cluster[i, j] = resource.ResourceToDemandConnection(stockCells[j], demands[i]);
                }
            }

            //-- Step 2: compute the total of Demand in front of each Stock
            float[] ds = new float[stocks.Count];
            for (int j = 0; j < stocks.Count; j++)
            {
                ds[j] = 0.0f;
                for (int i = 0; i < demands.Count; i++)
                {
                    {
                        ds[j] += cluster[i, j] * demands[i].GetDemandFor(resource.Id);
                    }
                }
            }

            //-- Step 3: compute the Cluster repartition factor of the Demand
            float[,] repartition = new float[demands.Count, stocks.Count];
            for (int i = 0; i < demands.Count; i++)
            {
                for (int j = 0; j < stocks.Count; j++)
                {
                    repartition[i, j] = cluster[i, j] * demands[i].GetDemandFor(resource.Id) / ds[j];
                }
            }

            //-- Step 4: compute the Stocks available for each Demand, and deduct corrective factors if we have too much
            float[,] available = new float[demands.Count, stocks.Count];
            float[] fc = new float[demands.Count];
            for (int i = 0; i < demands.Count; i++)
            {
                float demand_sum = 0.0f;
                for (int j = 0; j < stocks.Count; j++)
                {
                    float val = repartition[i, j] * stocks[j][resource.Id] * cluster[i, j];
                    available[i, j] = val;
                    demand_sum += val;
                }

                if (demand_sum > 0.0f)
                    fc[i] = Math.Min(1.0f, demands[i].GetDemandFor(resource.Id) / demand_sum);
                else
                    fc[i] = 1.0f;
            }

            //-- Step 5: compute the Allocation table
            float[,] allocationTable = new float[demands.Count, stocks.Count];
            for (int i = 0; i < demands.Count; i++)
            {
                for (int j = 0; j < stocks.Count; j++)
                {
                    allocationTable[i, j] = available[i, j] * fc[i];
                }
            }

            //-- Done: we now know for each demand how of each stock we'll allocate,
            //   so let's store it
            Allocation allocation = new Allocation(resource.Id, demands, stocks, allocationTable);
            AddAllocation(resource.Id, allocation);
        } //AllocateResourceToDemand

        public void AddAllocation(string resourceId, Allocation allocation)
        {
            Allocations[resourceId] = allocation;
        }

        public float Consume(string resourceId, Cell cell, float request)
        {
            if (Allocations.ContainsKey(resourceId))
                return Allocations[resourceId].Consume(cell, request);
            return 0.0f;
        }

        /// <summary>
        /// Return the amount that was allocated to this cell and for this Resource
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="demandCell"></param>
        /// <returns></returns>
        public float GetAllocation(string resourceId, Cell demandCell)
        {
            if (Allocations.ContainsKey(resourceId))
                return Allocations[resourceId].GetAllocation(demandCell);
            return 0.0f;
        }
    }


    /// <summary>
    /// Describe how a resource's stocks are going to be allocated
    /// to satisfy a demand. 
    /// </summary>
    public class Allocation
    {
        private string ResourceId { get; }
        private IList<IDictionary<string, float>> Stocks { get; }
        private IList<Cell> Demands { get; }

        private float[,] AllocationTable { get; set; }

        public Allocation(string resourceId, IList<Cell> demands, IList<IDictionary<string, float>> stocks,
            float[,] allocationTable)
        {
            ResourceId = resourceId;
            Demands = demands;
            Stocks = stocks;
            AllocationTable = allocationTable;
        }

        /// <summary>
        /// Consume up to requested amount
        /// </summary>
        /// <param name="requested">requested amount</param>
        /// <returns>The amount consumed</returns>
        public float Consume(Cell demandCell, float requested)
        {
            int i = Demands.IndexOf(demandCell);
            float consumed = 0.0f; 
            if (i >= 0)
            {
                for (int j = 0; j < Stocks.Count; j++)
                {
                    float conso = AllocationTable[i, j];
                    if (consumed + conso > requested) conso = requested - consumed;     // Do not consume more than requested
                    if (conso > Stocks[j][ResourceId]) conso = Stocks[j][ResourceId];   // Do not consume more than available
                    Stocks[j][ResourceId] -= conso;
                    consumed += conso;
                }
            }

            return consumed;
        }

        /// <summary>
        /// Return the amount that was allocated to this cell and for this Resource
        /// </summary>
        /// <param name="demandCell"></param>
        /// <returns></returns>
        public float GetAllocation(Cell demandCell)
        {
            int i = Demands.IndexOf(demandCell);
            float allocated = 0.0f; 
            if (i >= 0)
            {
                for (int j = 0; j < Stocks.Count; j++)
                {
                    allocated += AllocationTable[i, j];
                }
            }

            return allocated;
        }
    }
}
