using System;
using System.Collections;
using System.Collections.Generic;
using WorldSim.API;

namespace WorldSim.Model
{
    public class Allocator
    {
        public IDictionary<string, Allocation> Allocations { get; }

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
                switch (resource.Value.Distribution)
                {
                    case "nearest":
                        AllocateAsHarpagon(currentTime, (Resource) resource.Value, map);
                        break;
                    default:
                        AllocateAsSolomon(currentTime, (Resource) resource.Value, map);
                        break;
                }
            }
        }

        /// <summary>
        /// Allocates stocks using a nearest algorithm, i.e. a Demand tries to get
        /// as much as possible from the closest stock, and move further only if
        /// still not satisfied.
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="resourceValue"></param>
        /// <param name="map"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AllocateAsHarpagon(Time currentTime, Resource resource, Map map)
        {
            //-- Inits
            float totalDemand = 0.0f;
            float allocatedDemand = 0.0f;
            //-- Build the list of Stocks and Demands
            List<IDictionary<string, float>> stocks = new List<IDictionary<string, float>>();
            List<Cell> stockCells = new List<Cell>();
            List<Cell> demands = new List<Cell>();
            List<float> demandValues = new List<float>();
            foreach (var c in map.Cells)
            {
                Cell cell = (Cell) c;
                if (cell.GetStock(resource.Id) > 0.0f)
                {
                    stockCells.Add(cell);
                    stocks.Add(cell.Stocks);
                }

                float demand = cell.GetDemandFor(resource.Id);
                if (demand > 0.0f)
                {
                    demands.Add(cell);
                    totalDemand += demand;
                    demandValues.Add(demand);
                }
            }

            float[,] allocationTable = new float[demands.Count, stocks.Count];

            //-- For each distance from 0 to dmax, and as long as there are unsatisfied Demands
            int dmax = Math.Max(map.SizeX, map.SizeY);
            for (int distance = 0; (distance < dmax) && (allocatedDemand < totalDemand); distance++)
            {
                //-- For each non-fully allocated Stock which have one or more yet-unsatisfied Demands at this distance
                for (int j = 0; j < stockCells.Count; j++)
                {
                    Cell stockCell = stockCells[j];
                    float remainingStock = stocks[j][resource.Id] - AlreadyAllocatedStock(j, allocationTable);
                    if (remainingStock > 0.0f)
                    {
                        List<int> foundDemands = new List<int>();
                        for (int i = 0; i < demands.Count; i++)
                        {
                            if ((demands[i].DistanceTo(stockCell) == distance) && (AlreadyAllocatedDemand(i, allocationTable) < demandValues[i]))
                            {
                                foundDemands.Add(i);
                            }
                        }

                        //-- For each demand at this distance
                        foreach (int i in foundDemands)
                        {
                            //-- Allocate to this demand what's left of the stock / foundDemands.Count
                            // TODO: Some fair sharing could still happens at this level if a Stock is shared among several Demands.
                            float unsatisfiedDemand = demandValues[i] - AlreadyAllocatedDemand(i, allocationTable);
                            float allocated = Math.Min(remainingStock / foundDemands.Count, unsatisfiedDemand);
                            allocationTable[i, j] += allocated;
                            allocatedDemand += allocated;
                        }
                    }
                }
            }

            //-- Done: we now know for each demand how of each stock we'll allocate,
            //   so let's store it
            Allocation allocation = new Allocation(resource.Id, demands, stocks, allocationTable);
            AddAllocation(resource.Id, allocation);
        }//AllocateAsHarpagon

        private float AlreadyAllocatedDemand(int i, float[,] allocationTable)
        {
            //-- Sum what's has already bern allocated for a Demand
            float allocated = 0.0f;
            for (int j = 0; j <= allocationTable.GetUpperBound(1); j++)
            {
                allocated += allocationTable[i, j];
            }
            return allocated;
        }

        private static float AlreadyAllocatedStock(int j, float[,] allocationTable)
        {
            //-- Sum what's has already been allocated for a Stock
            float allocated = 0.0f;
            for (int i = 0; i < allocationTable.GetUpperBound(0); i++)
            {
                allocated += allocationTable[i, j];
            }

            return allocated;
        }

        /// <summary>
        /// Allocate the resource using a fair distribution,
        /// i.e. all stocks are spread evenly across all demands
        /// across the map on a pro-rata basis of the demands.
        /// If a cell is asking for X % of the total demand,
        /// it will allocated with X % of what's available.
        /// This code supports several distribution models:
        /// - spread: all stocks of the maps are spread evenly
        /// - local: only stocks up to some distance of demands can be used
        /// - attenuation: stocks availability decreases linearly with distance 
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="resource"></param>
        /// <param name="map"></param>
        private void AllocateAsSolomon(Time currentTime, Resource resource, Map map)
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
                    repartition[i, j] = ds[j] != 0.0f ? cluster[i, j] * demands[i].GetDemandFor(resource.Id) / ds[j] : 0.0f;
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
        } //AllocateAsSolomon

        public void AddAllocation(string resourceId, Allocation allocation)
        {
            Allocations[resourceId] = allocation;
        }

        public float Consume(string resourceId, Cell demandCell, float request)
        {
            if (Allocations.ContainsKey(resourceId))
                return Allocations[resourceId].Consume(demandCell, request);
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

        public float[,] AllocationTable { get; }

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
        /// <param name="demandCell">The Cell making the request</param>
        /// <param name="requested">Th requested amount</param>
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
                    if (consumed + conso > requested)
                        conso = requested - consumed; // Do not consume more than requested
                    if (conso > Stocks[j][ResourceId])
                        conso = Stocks[j][ResourceId]; // Do not consume more than available
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
