using System;
using System.Collections.Generic;

namespace WorldSim
{
    namespace API
    {
        /// <summary>
        ///     The <c>IWorld</c> interface represents the entire world. It id divided in 3 parts:
        ///     <list type="bullet">
        ///         <item>
        ///             <term>The Metadata part, which contains general information .</term>
        ///         </item>
        ///         <item>
        ///             <term>The background of the simulation, ie all the variables, resources, KPIs, etc.</term>
        ///         </item>
        ///         <item>
        ///             <term>Factory and tools methods to manage the various elements of the world.</term>
        ///         </item>
        ///     </list>
        /// </summary>
        public interface IWorld
        {
            //-- Metadata
            /// <summary>
            ///     Type of the simulation. For now, set to "scenario".
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            ///     Version of the Engine this simulation was created for or with.
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            ///     Modification date
            /// </summary>
            public DateTime ModDate { get; set; }

            /// <summary>
            ///     Various information about the author of this simulation.
            ///     The fields are completely free, you can put your name, e-mail, GitHub url, etc.
            /// </summary>
            public Dictionary<string, string> Author { get; set; }

            //-- Background
            /// <summary>
            ///     List of Units used in this simulation
            /// </summary>
            public Dictionary<string, IUnit> Units { get; }

            /// <summary>
            ///     List of Resources used in this simulation
            /// </summary>
            public Dictionary<string, IResource> Resources { get; }

            /// <summary>
            ///     List of KPIs used in this simulation
            /// </summary>
            public List<IKpi> Kpis { get; }

            /// <summary>
            ///     Time management class.
            ///     Moving back and forth through time is accomplished using this class.
            /// </summary>
            public ITime Time { get; }

            /// <summary>
            ///     The world is made of a rectangular map of Cells
            /// </summary>
            public IMap Map { get; }

            //-- Factories & Tools
            /// <summary>
            ///     Create a new Unit
            /// </summary>
            /// <param name="id">The unique ID of this Unit</param>
            /// <param name="name">The short name of this Unit</param>
            /// <param name="description">A longer description of this Unit</param>
            /// <param name="symbol">The symbol of this unit. Can be empty</param>
            /// <returns>A new Unit that can be added to the World's list of Units</returns>
            public IUnit CreateUnit(string id, string name, string description, string symbol);

            /// <summary>
            ///     Create a new Resource
            /// </summary>
            /// <param name="id">The unique ID of this Resource</param>
            /// <param name="name">The short name of this Resource</param>
            /// <param name="description">A longer description of this Resource</param>
            /// <param name="type">The type of the Resource. "stock" for now</param>
            /// <param name="unit">The Unit used by this Resource</param>
            /// <param name="distribution">How the Resource is available across the map. "spread" by default</param>
            /// <param name="range">If distribution="local", define the distance under which stocks can be used. 1 by default.
            ///                     If distribution="attenuation", define the distance where it gets the attenuation value</param>
            /// <param name="attenuation">The attenuation factor at range. Used only if distribution="attenuation"</param>
            /// <returns>A new Resource that can be added to the World's list of Resources</returns>
            public IResource CreateResource(string id, string name, string description, string type, IUnit? unit,
                string? distribution, int? range, float? attenuation);

            /// <summary>
            ///     Create a new KPI, i.e. something we are interested in tracking during the simulation
            /// </summary>
            /// <param name="name">The short name of this KPI</param>
            /// <param name="description">A longer description of this KPI</param>
            /// <param name="formula">Computation formula for this KPI</param>
            /// <param name="unit">The unit for this KPI (if any)</param>
            /// <returns>A new KIP that can be added to the World's list of KPIs</returns>
            public IKpi CreateKpi(string name, string description, string formula, IUnit? unit);

            /// <summary>
            ///     Create a new Map and add it to the World
            /// </summary>
            /// <param name="sizeX">Horizontal size of the Map</param>
            /// <param name="sizeY">Vertical size of the Map</param>
            public void CreateMap(int sizeX, int sizeY);

            /// <summary>
            ///     Create a JM2 by ID. See ICell and IJM2
            /// </summary>
            /// <param name="jm2Id">ID of the JM2 to create. Must correspond to something implemented in the Engine</param>
            /// <param name="init">Dictionary of initialization parameters for the JM2. This is very specific to each type of JM2.</param>
            /// <returns>A new JM2 that can be assigned to a Cell</returns>
            public IJM2 CreateJM2(string jm2Id, IDictionary<string, object> init);
        }

        /// <summary>
        ///     The <c>IUnit</c> interface describes a unit (of a physical quantity or anything else you like).
        /// </summary>
        public interface IUnit
        {
            /// <summary>
            ///     Unique ID of this Unit
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            ///     Short name of this Unit
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     Longer description of this Unit
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            ///     The symbol of this unit. Can be empty
            /// </summary>
            public string Symbol { get; set; }
        }

        /// <summary>
        ///     The <c>IResource</c> interface describes Resources (in a very general sense)
        ///     that are used in the simulation. These can be physical quantities or anything
        ///     else that you want to count and stock.
        /// </summary>
        public interface IResource
        {
            /// <summary>
            ///     Unique ID of this Resource
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            ///     Short name of this Resource
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     Longer description of this Resource
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            ///     Type of the Resource.
            ///     - "stock" by default: carries over each sim iteration
            ///     - "volatile" when recomputed from 0 at each sim iteration
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            ///     How the resource is available between cells
            ///     - "spread": the resource is available across the entire map
            ///     and will be evenly distributed among all demands,
            ///     on a pro-rata basis.
            ///     - "
            /// </summary>
            public string Distribution { get; set; }

            /// <summary>
            ///     ID of the Unit used by this Resource. See IUnit
            /// </summary>
            public IUnit? Unit { get; set; }

            /// <summary>
            /// Convert a value using this resource name and unit
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public string ValueToString(float value);
        }

        /// <summary>
        ///     The <c>IKpi</c> ("Key Performance Indicator") describes something
        ///     we are interested in tracking during the simulation
        /// </summary>
        public interface IKpi
        {
            /// <summary>
            ///     The short name of this KPI
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     Longer description of this KPI
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            ///     Computation formula for this KPI
            /// </summary>
            public string Formula { get; set; }

            /// <summary>
            ///     Unit used by this KPI
            /// </summary>
            public IUnit? Unit { get; set; }

            /// <summary>
            ///     Get the value of this KPI at the Current Time
            /// </summary>
            /// <param name="world">The world to compute the KPI on</param>
            /// <returns>Current Value</returns>
            public float GetValue(IWorld world);

            /// <summary>
            ///     Display a human-readable version of the KIP, with name + value + unit symbol
            /// </summary>
            /// <param name="map">The map to compute the KPI on</param>
            /// <param name="padding">Number of padding spaces to reserve to the left of the value</param>
            /// <returns></returns>
            public string ToString(IWorld world, int padding);
        }

        /// <summary>
        ///     The <c>TimeStep</c> enum describes the time interval used to iterate in the simulation: yearly, monthly or daily.
        ///     Note that all values like productions or consumptions are always defined on an annual basis, even if the
        ///     Time Step is monthly or daily. The Engine will adapt these values accordingly, and even accomodating for
        ///     leap years when daily steps are used.
        /// </summary>
        public enum TimeStep
        {
            year,
            month,
            day
        }

        /// <summary>
        ///     The <c>ITime</c> interface controls the flow of time during the simulation. This is how you move
        ///     the simulation forward, backward, fast-forward to a specific date, etc.
        /// </summary>
        public interface ITime
        {
            /// <summary>
            ///     Time interval used in the simulation. See the <c>TimeStep</c> class description
            /// </summary>
            public TimeStep StepUnit { get; set; }

            /// <summary>
            ///     Number of TimeStep to perform at each iteration. For example, a value of 3 with a monthly step
            ///     would mean a 3-month iteration.
            ///     Default value is 1.O
            /// </summary>
            public int StepValue { get; set; }

            /// <summary>
            ///     Start of the simulation
            /// </summary>
            public DateTime Start { get; set; }

            /// <summary>
            ///     End of the simulation
            /// </summary>
            public DateTime End { get; set; }

            /// <summary>
            ///     Current date of the simulation. Setting this value is how you "jump" directly to another date
            ///     in the future or in the past. The Current time cannot be moved before the Start date, but it
            ///     can be moved after the End date.
            /// </summary>
            public DateTime Current { get; set; }

            /// <summary>
            ///     Iteration counter, i.e. when Current == Start, iteration is 0.
            ///     Setting this value is another way to "jump" directly to another date in the future or in the past.
            ///     However the iteration cannot be set to a negative value.
            /// </summary>
            public int Iteration { get; set; }

            /// <summary>
            ///     Used to adjust annual productions and consumptions based on the Step Unit and Value.
            ///     For example, monthly Step with a value of 3 (= quarterly step) will give an Annual Divider of 4.0
            /// </summary>
            /// <returns>The value to use for dividing annual productions and consumptions</returns>
            public float GetAnnualDivider();

            /// <summary>
            ///     Restart the simulation its start. Same thing as writing Current = Start
            /// </summary>
            public void Restart();

            /// <summary>
            ///     Move the simulation forward by one iteration step.
            /// </summary>
            public void Step();

            /// <summary>
            ///     Move the simulation backward by one iteration step.
            ///     Important note: this means restarting from the beginning and replaying everything
            ///     until the previous step. So it will always be slower than moving one step forward.
            /// </summary>
            public void StepBack();

            /// <summary>
            ///     Helper method to test the Current time against a target date
            /// </summary>
            /// <param name="to">Target date to compare against</param>
            /// <returns>true if the Current time is past the target date</returns>
            public bool Reached(DateTime to)
            {
                return Current >= to;
            }

            /// <summary>
            ///     Helper method to test the current iteration against a target iteration index
            /// </summary>
            /// <param name="to"></param>
            /// <returns>true if the Current iteration index is past the target index</returns>
            public bool ReachedIteration(int to)
            {
                return Iteration >= to;
            }

            /// <summary>
            ///     Helper method testing if the Current time is past the End time, i.e. if the simulation is finished
            /// </summary>
            /// <returns></returns>
            public bool Done()
            {
                return Reached(End);
            }
        }

        /// <summary>
        ///     The <c>IMap</c> interface describes the World map, as an array of cells - see the ICell interface
        /// </summary>
        public interface IMap
        {
            /// <summary>
            ///     Horizontal size of the map
            /// </summary>
            public int SizeX { get; }

            /// <summary>
            ///     The vertical size of the map
            /// </summary>
            public int SizeY { get; }

            /// <summary>
            ///     The array of Cells. See ICell interface.
            /// </summary>
            public ICell[,] Cells { get; }

            public float TotalDemand(string resourceId);
            public float TotalStock(string resourceId);

        }

        /// <summary>
        ///     The <c>ICell</c> interface describe a cell in the Map.
        ///     Each Cell contains a stock value of each Resource.
        ///     It may also contain a JM2 or not (see the IJM2 interface).
        /// </summary>
        public interface ICell
        {
            /// <summary>
            ///     X-ccoordinate of the Cell
            /// </summary>
            public int X { get; }

            /// <summary>
            ///     Y-ccoordinate of the Cell
            /// </summary>
            public int Y { get; }

            /// <summary>
            ///     The JM2 located on the Cell. Can be null
            /// </summary>
            public IJM2 Jm2 { get; set; }

            /// <summary>
            ///     Resource Stock getter
            /// </summary>
            /// <param name="resourceId">Resource ID of the Cell stock</param>
            /// <returns></returns>
            public float GetStock(string resourceId);

            /// <summary>
            ///     Resource Stock setter
            /// </summary>
            /// <param name="resourceId">Resource ID of the Cell stock</param>
            /// <param name="stock">New stock value</param>
            public void SetStock(string resourceId, float stock);

            /// <summary>
            ///     Initial Resource Stock getter
            /// </summary>
            /// <param name="resourceId">Resource ID of the initial Cell stock</param>
            /// <returns></returns>
            public float GetInitialStock(string resourceId);

            /// <summary>
            ///     Initial Resource Stock setter.
            ///     This value will be applied to the Cell stock when the simulation is restarted
            /// </summary>
            /// <param name="resourceId">Resource ID of the initial Cell stock</param>
            /// <param name="stock">New stock value</param>
            public void SetInitialStock(string resourceId, float stock);

            /// <summary>
            ///     Human-readable details of the Cell
            /// </summary>
            /// <returns></returns>
            public string ToString();

            /// <summary>
            /// Give the cell the possibility to display extra lines of text in a cell
            /// </summary>
            /// <param name="extraLine">Line index</param>
            /// <returns></returns>
            public string GetExtraLine(int extraLine);

            /// <summary>
            /// Get the number of extra lines to display for this cell
            /// </summary>
            /// <returns></returns>
            int NbExtraLines();

            /// <summary>
            /// Allow the cell to add extra width to fit its extra lines
            /// </summary>
            /// <returns></returns>
            int ExtraWidth();
        }

        /// <summary>
        ///     A JM2 (aka "Jean-Marc Jancovici Machine") is a natural or artificial system that uses Resources
        ///     to transform the world, generally producing other Resources while doing so.
        ///     The IJM2 interface is a very abstract representation of such systems.
        ///     JM2s are created using the <c>IWorld.CreateJM2()</c> factory method.
        /// </summary>
        public interface IJM2
        {
            /// <summary>
            ///     Identifies the type of JM2: source, sink, factory, etc.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            ///     The efficiency of the JM2 the last time is was run.
            ///     Between 0.0 (0%) and 1.0 (100%).
            ///     Will be null if it does make sense for this JM2.
            /// </summary>
            public float? Efficiency { get; }


            /// <summary>
            /// Give the JM2 the possibility to display extra lines of text in a cell
            /// </summary>
            /// <param name="extraLine">Line index</param>
            /// <returns></returns>
            string GetExtraLine(int extraLine);

            /// <summary>
            /// Get the number of extra lines to display for this JM2
            /// </summary>
            /// <returns></returns>
            public int NbExtraLines();

            /// <summary>
            /// Allow the JM2 to add extra width to fit its extra lines
            /// </summary>
            /// <returns></returns>
            int ExtraWidth();
        }
    }
}
