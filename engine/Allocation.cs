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
            foreach (var r in resources)
            {
                Allocation allocation;
                Resource resource = (Resource) r.Value;
                switch (resource.Distribution)
                {
                    case "nearest":
                        allocation = Allocation.AllocateAsHarpagon(currentTime, resource, map.Cells,
                            Math.Max(map.SizeX, map.SizeY));
                        break;
                    default:
                        allocation = Allocation.AllocateAsSolomon(currentTime, resource, map.Cells);
                        break;
                }

                Allocations[resource.Id] = allocation;
            }
        }


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
        /// Allocate the resource using a fair distribution,
        /// i.e. all stocks are spread evenly across all demands
        /// across the list of cells on a pro-rata basis of the demands.
        /// If a cell is asking for X % of the total demand,
        /// it will allocated with X % of what's available.
        /// This code supports several distribution models:
        /// - spread: all stocks of the maps are spread evenly
        /// - local: only stocks up to some distance of demands can be used
        /// - attenuation: stocks availability decreases linearly with distance 
        /// Return the Allocation for this Resource
        /// </summary>
        /// <param name="currentTime">Current simulation Time</param>
        /// <param name="resource"></param>
        /// <param name="cells">List of Cells</param>
        /// <returns>Allocation</returns>
        public static Allocation AllocateAsSolomon(Time currentTime, Resource resource, IEnumerable cells)
        {
            //-- Build the list of Stocks and Demands
            AllocationContext context = new AllocationContext();
            context.Build(resource, cells);

            //-- Step 1: build the Clusters table
            float[,] cluster = new float[context.Demands.Count, context.Stocks.Count];
            for (int i = 0; i < context.Demands.Count; i++)
            {
                for (int j = 0; j < context.Stocks.Count; j++)
                {
                    cluster[i, j] = resource.ResourceToDemandConnection(context.StockCells[j], context.Demands[i]);
                }
            }

            //-- Steps 2, 3, 4 and 5:
            float[,] allocationTable = SolomonSpread(cluster, context.StockValues, context.DemandValues);
            
            //-- Done: we now know for each demand how much of each stock we'll allocate
            return new Allocation(resource.Id, context.Demands, context.Stocks, allocationTable);
        }

        public static float[,] SolomonSpread(float[,] cluster, List<float> stockValues, List<float> demandValues)
        {
            //-- Step 2: compute the total of Demand in front of each Stock
            float[] ds = new float[stockValues.Count];
            for (int j = 0; j < stockValues.Count; j++)
            {
                ds[j] = 0.0f;
                for (int i = 0; i < demandValues.Count; i++)
                {
                    {
                        ds[j] += cluster[i, j] * demandValues[i];
                    }
                }
            }

            //-- Step 3: compute the Cluster repartition factor of the Demand
            float[,] repartition = new float[demandValues.Count, stockValues.Count];
            for (int i = 0; i < demandValues.Count; i++)
            {
                for (int j = 0; j < stockValues.Count; j++)
                {
                    repartition[i, j] =
                        ds[j] != 0.0f ? cluster[i, j] * demandValues[i] / ds[j] : 0.0f;
                }
            }

            //-- Step 4: compute the Stocks available for each Demand, and deduct corrective factors if we have too much
            float[,] available = new float[demandValues.Count, stockValues.Count];
            float[] fc = new float[demandValues.Count];
            for (int i = 0; i < demandValues.Count; i++)
            {
                float demandSum = 0.0f;
                for (int j = 0; j < stockValues.Count; j++)
                {
                    float val = repartition[i, j] * stockValues[j] * cluster[i, j];
                    available[i, j] = val;
                    demandSum += val;
                }

                if (demandSum > 0.0f)
                    fc[i] = Math.Min(1.0f, demandValues[i] / demandSum);
                else
                    fc[i] = 1.0f;
            }

            //-- Step 5: compute the Allocation table
            float[,] allocationTable = new float[demandValues.Count, stockValues.Count];
            for (int i = 0; i < demandValues.Count; i++)
            {
                for (int j = 0; j < stockValues.Count; j++)
                {
                    allocationTable[i, j] = available[i, j] * fc[i];
                }
            }

            return allocationTable;
        }

        /// <summary>
        /// Allocates stocks using a nearest algorithm, i.e. a Demand tries to get
        /// as much as possible from the closest stock, and move further only if
        /// still not satisfied.
        /// </summary>
        /// <param name="currentTime">Current simulation Time</param>
        /// <param name="resource"></param>
        /// <param name="cells">List of Cells</param>
        /// <returns>Allocation</returns>
        public static Allocation AllocateAsHarpagon(Time currentTime, Resource resource, IEnumerable cells, int dmax)
        {
            //-- Inits
            float allocatedDemand = 0.0f;
            AllocationContext context = new AllocationContext();
            context.Build(resource, cells);

            float[,] allocationTable = new float[context.Demands.Count, context.Stocks.Count];

            //-- For each distance from 0 to dmax, and as long as there are unsatisfied Demands
            for (int distance = 0; (distance < dmax) && (allocatedDemand < context.TotalDemand); distance++)
            {
                //-- For each non-fully allocated Stock which have one or more yet-unsatisfied Demands at this distance
                for (int j = 0; j < context.StockCells.Count; j++)
                {
                    Cell stockCell = context.StockCells[j];
                    float remainingStock = context.Stocks[j][resource.Id] - AlreadyAllocatedStock(j, allocationTable);
                    if (remainingStock > 0.0f)
                    {
                        List<int> foundDemands = new List<int>();
                        for (int i = 0; i < context.Demands.Count; i++)
                        {
                            if ((context.Demands[i].DistanceTo(stockCell) == distance) &&
                                (AlreadyAllocatedDemand(i, allocationTable) < context.DemandValues[i]))
                            {
                                foundDemands.Add(i);
                            }
                        }

                        //-- For each demand at this distance
                        foreach (int i in foundDemands)
                        {
                            //-- Allocate to this demand what's left of the stock / foundDemands.Count
                            // TODO: Some fair sharing could still happens at this level if a Stock is shared among several Demands.
                            float unsatisfiedDemand =
                                context.DemandValues[i] - AlreadyAllocatedDemand(i, allocationTable);
                            float allocated = Math.Min(remainingStock / foundDemands.Count, unsatisfiedDemand);
                            allocationTable[i, j] += allocated;
                            allocatedDemand += allocated;
                        }
                    }
                }
            }

            //-- Done: we now know for each demand how much of each stock we'll allocate
            return new Allocation(resource.Id, context.Demands, context.Stocks, allocationTable);
        } //AllocateAsHarpagon

        private static float AlreadyAllocatedDemand(int i, float[,] allocationTable)
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

    /// <summary>
    /// Container of the various pieces that are needed to perform an allocation
    /// </summary>
    public class AllocationContext
    {
        public List<IDictionary<string, float>> Stocks { get; }
        public List<Cell> StockCells { get; }
        public List<float> StockValues { get; }
        public List<Cell> Demands { get; }
        public List<float> DemandValues { get; }
        public float TotalStock { get; private set; }
        public float TotalDemand { get; private set; }

        public AllocationContext()
        {
            Stocks = new List<IDictionary<string, float>>();
            StockCells = new List<Cell>();
            StockValues = new List<float>();
            Demands = new List<Cell>();
            DemandValues = new List<float>();
            TotalStock = 0.0f;
            TotalDemand = 0.0f;
        }

        public void Build(Resource resource, IEnumerable cells)
        {
            foreach (var c in cells)
            {
                Cell cell = (Cell) c;
                float stock = cell.GetStock(resource.Id);
                if (stock > 0.0f)
                {
                    StockCells.Add(cell);
                    Stocks.Add(cell.Stocks);
                    StockValues.Add(stock);
                    TotalStock += stock;
                }

                float demand = cell.GetDemandFor(resource.Id);
                if (demand > 0.0f)
                {
                    Demands.Add(cell);
                    TotalDemand += demand;
                    DemandValues.Add(demand);
                }
            }
        }
    }
}
