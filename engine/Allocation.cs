using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldSim.Engine
{
    /// <summary>
    /// Describe how a resource's stocks are going to be allocated
    /// to satisfy a demand. 
    /// </summary>
    public class Allocation
    {
        public string ResourceId { get; }
        public string TargetId { get;  }
        public float Total { get; set; }
        public IList<Assignment> Assignments { get; }

        public Allocation(string resourceId, string targetId)
        {
            ResourceId = resourceId;
            TargetId = targetId;
            Total = 0.0f;
            Assignments = new List<Assignment>();
        }

        public void Assign(float amount, IDictionary<string, float> stocks)
        {
            Assignment assignment = new Assignment(ResourceId, amount, stocks);
            Assignments.Add(assignment);
            Total += amount;
        }

        /// <summary>
        /// Consume a requested amount of an allocated resource.
        /// Note that the resource's stocks are impacted.
        /// </summary>
        /// <param name="requested">requested amount</param>
        /// <returns>The amount consumed</returns>
        public float Consume(float requested)
        {
            float consumed = 0.0f;
            requested = Math.Min(requested, Total); // We won't take more than what was allocated
            foreach (Assignment assignment in Assignments)
            {
                if (requested > 0.0f)
                {
                    consumed += assignment.Consume(requested);
                    requested -= consumed;
                }
            }

            return consumed;
        }
    }

    public class Assignment
    {
        public string ResourceId { get; }
        public float Amount { get; }
        private IDictionary<string, float> Stocks { get; }

        public Assignment(string resourceId, float amount, IDictionary<string, float> stocks)
        {
            ResourceId = resourceId;
            Amount = amount;
            Stocks = stocks;
        }

        /// <summary>
        /// Consume up to requested amount
        /// </summary>
        /// <param name="requested">requested amount</param>
        /// <returns>The amount consumed</returns>
        public float Consume(float requested)
        {
            float usable = Math.Min(requested, Amount); // We won't take more than what was allocated
            float consumed = Math.Min(Stocks[ResourceId], usable); // We won't take more than the available stock
            Stocks[ResourceId] -= consumed;
            return consumed;
        }
    }
}
