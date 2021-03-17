using System;
using System.Collections.Generic;
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
        private Time _time;
        private Map _map;

        [SetUp]
        public void Setup()
        {
            Unit unit = new Unit("mass", "Mass", "Metric Tons", "T");
            _units = new Dictionary<string, IUnit>();
            _units.Add("mass", unit);

            _resources = new Dictionary<string, IResource>();
            Resource resource = new Resource("coal", "Coal", "Coal is bad", "stock", unit);
            _resources.Add("coal", resource);
            
            _stocks = new Dictionary<string, float>()
            {
                {"coal", 0.0f}
            };

            _time = new Time(null);
            
            _map = new Map(1, 1);
            _map.Init(_resources);
            _map.Cells[0, 0].SetStock("coal", 100.0f);
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

            Dictionary<string, float> output = new Dictionary<string, float>();
            jm2.Step(_map, _stocks, _time, 1.0f, output);

            Assert.AreEqual(100.0f, output["coal"]);
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

            Dictionary<string, float> output = new Dictionary<string, float>();
            jm2.Step(_map, _stocks, _time, 1.0f, output);

            Assert.AreEqual(50.0f, output["coal"]);
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

            Dictionary<string, float> output = new Dictionary<string, float>();

            for (int i = 0; i < 4; i++)
            {
                jm2.Step(_map, _stocks, _time, 1.0f, output);
                Assert.AreEqual(100.0f, output["coal"]);
                _stocks["coal"] += output["coal"];
            }
            Assert.AreEqual(400.0f, _stocks["coal"]);
            jm2.Step(_map, _stocks, _time, 1.0f, output);
            Assert.AreEqual(0.0f, output["coal"]);  // Max level is reached, production should stop

            _stocks["coal"] -= 150.0f;
            jm2.Step(_map, _stocks, _time, 1.0f, output);
            Assert.AreEqual(0.0f, output["coal"]);  // Level is under max but sill above min => still no production

            _stocks["coal"] -= 100.0f;
            jm2.Step(_map, _stocks, _time, 1.0f, output);
            _stocks["coal"] += output["coal"];
            Assert.AreEqual(100.0f, output["coal"]);  // Level is under min => production resumed
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
            jm2.Step(_map, _stocks, _time, 1.0f, output);

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

            Dictionary<string, float> output = new Dictionary<string, float>();

            for (int i = 0; i < 4; i++)
            {
                jm2.Step(_map, _stocks, _time, 1.0f, output);
                _stocks["coal"] += output["coal"];
                _time.Step();
            }
            Assert.AreEqual(600.0f, jm2.Reserve());
            
            jm2.Step(_map, _stocks, _time, 1.0f, output);
            _stocks["coal"] += output["coal"];
            _time.Step();
            Assert.AreEqual(100.0f, output["coal"]);
            Assert.AreEqual(520.0f, jm2.Reserve());
            Assert.AreEqual(500.0f, _stocks["coal"]);
            
            for (int i = 6; i < 12; i++)
            {
                jm2.Step(_map, _stocks, _time, 1.0f, output);
                _stocks["coal"] += output["coal"];
                _time.Step();
            }
            Assert.AreEqual(40.0f, jm2.Reserve());
            Assert.AreEqual(100.0f, output["coal"]);

            jm2.Step(_map, _stocks, _time, 1.0f, output);
            _stocks["coal"] += output["coal"];
            _time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(60.0f, output["coal"]);

            for (int i = 13; i < 16; i++)
            {
                jm2.Step(_map, _stocks, _time, 1.0f, output);
                _stocks["coal"] += output["coal"];
                _time.Step();
                Assert.AreEqual(0.0f, jm2.Reserve());
                Assert.AreEqual(20.0f, output["coal"]);
            }

            jm2.Step(_map, _stocks, _time, 1.0f, output);
            _stocks["coal"] += output["coal"];
            _time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(12.0f, output["coal"]);
            
            jm2.Step(_map, _stocks, _time, 1.0f, output);
            _stocks["coal"] += output["coal"];
            _time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(4.0f, output["coal"]);
        }

        [Test]
        public void TestSink()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"consumption", 100.0f}
            };
            JM2Sink jm2 = new JM2Sink(init);
            Assert.AreEqual("sink", jm2.Id);

            Cell cell = (Cell) _map.Cells[0, 0];
            cell.Jm2 = jm2;
            Assert.AreEqual(100.0f, cell.GetStock("coal"));
            cell.StepPrepare(_time);
            cell.StepExecute(_map, _time, 1.0f);
            cell.StepFinalize(_time);
            Assert.AreEqual(0.0f, cell.GetStock("coal")); // Because we reached the limit
            Assert.AreEqual(1.0f, jm2.Efficiency);
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

            Cell cell = (Cell) _map.Cells[0, 0];
            cell.Jm2 = jm2;
            Assert.AreEqual(100.0f, cell.GetStock("coal"));
            cell.StepPrepare(_time);
            cell.StepExecute(_map, _time, 1.0f);
            cell.StepFinalize(_time);
            Assert.AreEqual(50.0f, cell.GetStock("coal")); // Because we reached the limit
            Assert.AreEqual(0.5f, jm2.Efficiency);
        }
    }
}