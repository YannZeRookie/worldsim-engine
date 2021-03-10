using System;
using System.Collections.Generic;
using NUnit.Framework;
using WorldSim.API;
using WorldSim.Engine;

namespace WorldSim.Engine.Tests
{
    [TestFixture]
    public class JM2Tests
    {
        private World _world;

        [SetUp]
        public void Setup()
        {
            _world = new World();
            Unit unit = new Unit("mass", "Mass", "Metric Tons", "T");
            _world.Units.Add("mass", unit);
            Resource resource = new Resource("coal", "Coal", "Coal is bad", "stock", "mass");
            _world.Resources.Add("coal", resource);
            Time time = new Time(_world);
            _world.CreateMap(1, 1);
            _world.Map.Cells[0, 0].SetStock("coal", 100.0f);
        }

        [Test]
        public void TestSource()
        {
            IDictionary<string, object> init = new Dictionary<string, object>()
            {
                {"resource_id", "coal"},
                {"production", 100.0f}
            };
            Time time = new Time(null);
            JM2Source jm2 = new JM2Source(init);
            Assert.AreEqual("source", jm2.Id);

            Dictionary<string, float> stocks = new Dictionary<string, float>()
            {
                {"coal", 0.0f}
            };
            Dictionary<string, float> output = new Dictionary<string, float>();
            jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);

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
            Time time = new Time(null);
            JM2Source jm2 = new JM2Source(init);
            Assert.AreEqual("source", jm2.Id);

            Dictionary<string, float> stocks = new Dictionary<string, float>()
            {
                {"coal", 0.0f}
            };
            Dictionary<string, float> output = new Dictionary<string, float>();
            jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);

            Assert.AreEqual(50.0f, output["coal"]);
            Assert.AreEqual(0.5f, jm2.Efficiency);
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
            Time time = new Time(null);
            JM2Mine jm2 = new JM2Mine(init);
            Assert.AreEqual("mine", jm2.Id);

            Dictionary<string, float> stocks = new Dictionary<string, float>()
            {
                {"coal", 0.0f}
            };
            Dictionary<string, float> output = new Dictionary<string, float>();
            jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);

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
            Time time = new Time(null);
            JM2Mine jm2 = new JM2RecyclingMine(init);
            Assert.AreEqual("recycling_mine", jm2.Id);

            Dictionary<string, float> stocks = new Dictionary<string, float>()
            {
                {"coal", 0.0f}
            };
            Dictionary<string, float> output = new Dictionary<string, float>();

            for (int i = 0; i < 4; i++)
            {
                jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);
                stocks["coal"] += output["coal"];
                time.Step();
            }
            Assert.AreEqual(600.0f, jm2.Reserve());
            
            jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);
            stocks["coal"] += output["coal"];
            time.Step();
            Assert.AreEqual(100.0f, output["coal"]);
            Assert.AreEqual(520.0f, jm2.Reserve());
            Assert.AreEqual(500.0f, stocks["coal"]);
            
            for (int i = 6; i < 12; i++)
            {
                jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);
                stocks["coal"] += output["coal"];
                time.Step();
            }
            Assert.AreEqual(40.0f, jm2.Reserve());
            Assert.AreEqual(100.0f, output["coal"]);

            jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);
            stocks["coal"] += output["coal"];
            time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(60.0f, output["coal"]);

            for (int i = 13; i < 16; i++)
            {
                jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);
                stocks["coal"] += output["coal"];
                time.Step();
                Assert.AreEqual(0.0f, jm2.Reserve());
                Assert.AreEqual(20.0f, output["coal"]);
            }

            jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);
            stocks["coal"] += output["coal"];
            time.Step();
            Assert.AreEqual(0.0f, jm2.Reserve());
            Assert.AreEqual(12.0f, output["coal"]);
            
            jm2.Step((Map) _world.Map, stocks, time, 1.0f, output);
            stocks["coal"] += output["coal"];
            time.Step();
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
            Time time = new Time(null);
            JM2Sink jm2 = new JM2Sink(init);
            Assert.AreEqual("sink", jm2.Id);

            Cell cell = (Cell) _world.Map.Cells[0, 0];
            cell.Jm2 = jm2;
            Assert.AreEqual(100.0f, cell.GetStock("coal"));
            cell.StepPrepare(time);
            cell.StepExecute(time, 1.0f);
            cell.StepFinalize(time);
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
            Time time = new Time(null);
            JM2Sink jm2 = new JM2Sink(init);
            Assert.AreEqual("sink", jm2.Id);

            Cell cell = (Cell) _world.Map.Cells[0, 0];
            cell.Jm2 = jm2;
            Assert.AreEqual(100.0f, cell.GetStock("coal"));
            cell.StepPrepare(time);
            cell.StepExecute(time, 1.0f);
            cell.StepFinalize(time);
            Assert.AreEqual(50.0f, cell.GetStock("coal")); // Because we reached the limit
            Assert.AreEqual(0.5f, jm2.Efficiency);
        }
    }
}