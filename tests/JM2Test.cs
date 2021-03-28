using System;
using System.Collections.Generic;
using System.Linq;
using ChoETL;
using NUnit.Framework;
using WorldSim.API;

namespace WorldSim.Engine.Tests
{
    [TestFixture]
    public class JM2Tests
    {
        private IDictionary<string, IUnit> _units;
        private IDictionary<string, IResource> _resources;
        private IDictionary<string, float> _stocks;
        private IDictionary<string, float> _output;
        private IDictionary<string, float> _demand;
        private IDictionary<string, Allocation> _allocations;
        private Time _time;

        [SetUp]
        public void Setup()
        {
            Unit unit = new Unit("mass", "Mass", "Metric Tons", "T");
            _units = new Dictionary<string, IUnit>();
            _units.Add("mass", unit);

            _resources = new Dictionary<string, IResource>();
            SetupOneResource("coal", "Coal", "Coal is bad", "", unit, "");
            SetupOneResource("o2", "Oxygen", "Main oxide", "", _units["mass"], "");
            SetupOneResource("co2", "Carbon Dioxide", "The root of climate warming", "", _units["mass"], "");

            _stocks = new Dictionary<string, float>()
            {
                {"coal", 100.0f},
                {"o2", 10000.0f},
                {"co2", 0.0f}
            };
            _allocations = new Dictionary<string, Allocation>();
            _demand = new Dictionary<string, float>();
            _output = new Dictionary<string, float>();

            _time = new Time(null);
        }

        private void SetupOneResource(string id, string name, string description, string type, IUnit? unit, string distribution)
        {
            Resource resource = new Resource(id, name, description, type, unit, distribution);
            _resources.Add(id, resource);
        }

        private void Allocate(string resourceId, float amount)
        {
            Allocation allocation = new Allocation(resourceId, "");
            allocation.Assign(amount, _stocks);
            _allocations[resourceId] = allocation;
        }

        [Test]
        public void TestSource()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"production", 100.0f}
            };
            JM2Source jm2 = new JM2Source(init);
            Assert.AreEqual("source", jm2.Id);

            jm2.Step(_stocks, _time, _allocations, _output);

            Assert.AreEqual(100.0f, _output["coal"]);
            Assert.AreEqual(1.0f, jm2.Efficiency);
        }

        [Test]
        public void TestSourceWithReserve()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"reserve", 50.0f},
                {"production", 100.0f}
            };
            JM2Source jm2 = new JM2Source(init);
            Assert.AreEqual("source", jm2.Id);

            jm2.Step(_stocks, _time, _allocations, _output);

            Assert.AreEqual(50.0f, _output["coal"]);
            Assert.AreEqual(0.5f, jm2.Efficiency);
        }

        [Test]
        public void TestSourceMinMax()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"reserve", 1000.0f},
                {"production", 100.0f},
                {"levelMin", 200.0f},
                {"levelMax", 400.0f},
            };
            JM2Source jm2 = new Jm2SourceMinMax(init);
            Assert.AreEqual("sourceMinMax", jm2.Id);
            _stocks["coal"] = 0.0f;

            for (int i = 0; i < 4; i++)
            {
                jm2.Step(_stocks, _time, _allocations, _output);
                Assert.AreEqual(100.0f, _output["coal"]);
                _stocks["coal"] += _output["coal"];
            }

            Assert.AreEqual(400.0f, _stocks["coal"]);
            jm2.Step(_stocks, _time, _allocations, _output);
            Assert.AreEqual(0.0f, _output["coal"]); // Max level is reached, production should stop

            _stocks["coal"] -= 150.0f;
            jm2.Step(_stocks, _time, _allocations, _output);
            Assert.AreEqual(0.0f, _output["coal"]); // Level is under max but sill above min => still no production

            _stocks["coal"] -= 100.0f;
            jm2.Step(_stocks, _time, _allocations, _output);
            _stocks["coal"] += _output["coal"];
            Assert.AreEqual(100.0f, _output["coal"]); // Level is under min => production resumed
            Assert.AreEqual(250.0f, _stocks["coal"]);
        }

        [Test]
        public void TestMine()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"reserve", 200.0f},
                {"production", 100.0f}
            };
            JM2Mine jm2 = new JM2Mine(init);
            Assert.AreEqual("mine", jm2.Id);

            Dictionary<string, float> output = new Dictionary<string, float>();
            jm2.Step(_stocks, _time, _allocations, output);

            Assert.AreEqual(100.0f, output["coal"]);
            Assert.AreEqual(1.0f, jm2.Efficiency);
        }

        [Test]
        public void TestRecyclingMine()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"reserve", 1000.0f},
                {"production", 100.0f},
                {"time_in_use", 4},
                {"recycling", 0.2f}
            };
            JM2Mine jm2 = new JM2RecyclingMine(init);
            Assert.AreEqual("recycling_mine", jm2.Id);
            _stocks["coal"] = 0.0f;

            for (int i = 0; i < 4; i++)
            {
                jm2.Step(_stocks, _time, _allocations, _output);
                _stocks["coal"] += _output["coal"];
                _time.Step();
            }

            Assert.AreEqual(600.0f, jm2.Reserve());

            jm2.Step(_stocks, _time, _allocations, _output);
            _stocks["coal"] += _output["coal"];
            _time.Step();
            Assert.AreEqual(100.0f, _output["coal"]);
            Assert.AreEqual(520.0f, jm2.Reserve());
            Assert.AreEqual(500.0f, _stocks["coal"]);

            for (int i = 6; i < 12; i++)
            {
                jm2.Step(_stocks, _time, _allocations, _output);
                _stocks["coal"] += _output["coal"];
                _time.Step();
            }

            Assert.AreEqual(40.0f, jm2.Reserve());
            Assert.AreEqual(100.0f, _output["coal"]);

            jm2.Step(_stocks, _time, _allocations, _output);
            _stocks["coal"] += _output["coal"];
            _time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(60.0f, _output["coal"]);

            for (int i = 13; i < 16; i++)
            {
                jm2.Step(_stocks, _time, _allocations, _output);
                _stocks["coal"] += _output["coal"];
                _time.Step();
                Assert.AreEqual(0.0f, jm2.Reserve());
                Assert.AreEqual(20.0f, _output["coal"]);
            }

            jm2.Step(_stocks, _time, _allocations, _output);
            _stocks["coal"] += _output["coal"];
            _time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(12.0f, _output["coal"]);

            jm2.Step(_stocks, _time, _allocations, _output);
            _stocks["coal"] += _output["coal"];
            _time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(4.0f, _output["coal"]);
        }

        [Test]
        public void TestSink()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"consumption", 75.0f}
            };
            JM2Sink jm2 = new JM2Sink(init);
            Assert.AreEqual("sink", jm2.Id);

            Allocation allocation = new Allocation("coal", "");
            allocation.Assign(100.0f, _stocks);
            _allocations["coal"] = allocation;

            jm2.DescribeDemand(_time, _demand);
            Assert.AreEqual(75.0f, _demand["coal"]);

            jm2.Step(_stocks, _time, _allocations, _output);

            Assert.AreEqual(25.0f, _stocks["coal"]);
            Assert.AreEqual(1.0f, jm2.Efficiency);

            // One more time: reach 0 stock
            // Note that we allocate more than what is available, it's OK, the code is robust enough
            jm2.Step(_stocks, _time, _allocations, _output);
            Assert.AreEqual(0.0f, _stocks["coal"]);
            Assert.AreEqual(25.0f / 75.0f, jm2.Efficiency);
        }

        [Test]
        public void TestSinkWithLowAllocation()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"consumption", 100.0f}
            };
            JM2Sink jm2 = new JM2Sink(init);
            Assert.AreEqual("sink", jm2.Id);
            Allocate("coal", 50.0f);

            jm2.DescribeDemand(_time, _demand);
            Assert.AreEqual(100.0f, _demand["coal"]);

            jm2.Step(_stocks, _time, _allocations, _output);

            Assert.AreEqual(50.0f, _stocks["coal"]);
            Assert.AreEqual(0.5f, jm2.Efficiency);
        }

        [Test]
        public void TestSinkWithLimit()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"limit", 50},
                {"consumption", 100.0f}
            };
            JM2Sink jm2 = new JM2Sink(init);
            Assert.AreEqual("sink", jm2.Id);
            Allocate("coal", 50.0f);

            jm2.Step(_stocks, _time, _allocations, _output);
            
            Assert.AreEqual(50.0f, _stocks["coal"]); // Because we reached the limit
            Assert.AreEqual(0.5f, jm2.Efficiency);
        }

        [Test]
        public void TestFactory()
        {
            string yaml = @"
opex:
  - resource_id: coal
    consumption: 50
  - resource_id: o2
    consumption: 1000
output:
  - resource_id: co2
    production: 1833
";
            var parser = ChoYamlReader<Dictionary<string, object>>.LoadText(yaml);
            JM2Factory jm2 = new JM2Factory(parser.First());
            Assert.AreEqual("factory", jm2.Id);
            Allocate("coal", 50.0f);
            Allocate("o2", 1000.0f);

            jm2.Step(_stocks, _time, _allocations, _output);

            Assert.AreEqual(1833.0f, _output["co2"]);
            Assert.AreEqual(50.0f, _stocks["coal"]);
            Assert.AreEqual(9000.0f, _stocks["o2"]);
            Assert.AreEqual(1.0f, jm2.Efficiency);
        }

        [Test]
        public void TestLimitedFactory()
        {
            string yaml = @"
opex:
  - resource_id: coal
    consumption: 50
  - resource_id: o2
    consumption: 1000
output:
  - resource_id: co2
    production: 1800
";
            var parser = ChoYamlReader<Dictionary<string, object>>.LoadText(yaml);
            JM2Factory jm2 = new JM2Factory(parser.First());
            Assert.AreEqual("factory", jm2.Id);
            Allocate("coal", 25.0f);    // We assign less than requested
            Allocate("o2", 1000.0f);

            jm2.Step(_stocks, _time, _allocations, _output);

            Assert.AreEqual(900.0f, _output["co2"]);
            Assert.AreEqual(75.0f, _stocks["coal"]);
            Assert.AreEqual(9500.0f, _stocks["o2"]);
            Assert.AreEqual(0.5f, jm2.Efficiency);
        }
    }
}
