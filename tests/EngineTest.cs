using System;
using NUnit.Framework;
using WorldSim.API;
using WorldSim.Engine;

namespace WorldSim.Engine.Tests
{
    [TestFixture]
    public class EngineTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void LoadSimple01Yaml()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            //-- Header
            Assert.NotNull(engine.World);
            Assert.AreEqual("scenario", engine.World.Type);
            Assert.AreEqual(new DateTime(2021, 01, 02, 10, 57, 43), engine.World.ModDate);
            Assert.AreEqual("Yann Corno", engine.World.Author["name"]);
            //-- Background
            Assert.IsNotNull(engine.World.Units["mass-t"]);
            Assert.IsFalse(engine.World.Units.ContainsKey("toto"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
            {
                var a = engine.World.Units["toto"];
            });
            Assert.IsNotNull(engine.World.Resources["coal"]);
            Assert.AreEqual("mass-t", engine.World.Resources["coal"].UnitId);

            IKeyAttributes kpi = engine.World.KeyAttributes[0];
            Assert.AreEqual("Coal Stock", kpi.Name);
            Assert.AreEqual("mass-t", kpi.UnitId);

            Assert.AreEqual(new DateTime(1800, 1, 1), engine.World.Time.Start);
            Assert.AreEqual(new DateTime(2200, 1, 1), engine.World.Time.End);
            Assert.AreEqual(new DateTime(1965, 1, 1), engine.World.Time.Current);

            Assert.AreEqual(1, engine.World.Map.SizeX);
            Assert.AreEqual(1, engine.World.Map.SizeY);
            Assert.IsNotNull(engine.World.Map.Cells[0, 0]);
            Assert.AreEqual("mine", engine.World.Map.Cells[0, 0].Jm2.Id);

            Assert.AreEqual(165000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));

            //-- Check cell content
            engine.World.Time.Restart();
            Assert.AreEqual(engine.World.Time.Start, engine.World.Time.Current);
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
        }

        [Test]
        public void LoadAndDontRunSimple01Yaml()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml", true);
            Assert.AreEqual(new DateTime(1800, 1, 1), engine.World.Time.Start);
            Assert.AreEqual(engine.World.Time.Start, engine.World.Time.Current);
        }

        /// <summary>
        /// Test that we can read the format demo file
        /// </summary>
        [Test]
        public void LoadFormatYaml()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../../engine/doc/format.yaml");
            Assert.NotNull(engine.World);
        }

        [Test]
        public void TestKeyAttributes()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            IKeyAttributes kpi = engine.World.KeyAttributes[0];
            Assert.AreEqual("Coal Stock", kpi.Name);
            engine.World.Time.Restart();
            engine.World.Time.Step();
            Assert.AreEqual(1000.0f, kpi.GetValue());
        }

        [Test]
        public void RunSimple01Yaml()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            engine.World.Time.Restart();
            Assert.AreEqual(0, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1800, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            engine.World.Time.Step();
            Assert.AreEqual(1, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1801, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(1.0e3f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            engine.World.Time.Step();
            Assert.AreEqual(2.0e3f, engine.World.Map.Cells[0, 0].GetStock("coal"));
        }

        [Test]
        public void TestTimeTravelSimple01()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            //-- Current time
            Assert.AreEqual(165, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1965, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(165000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            //-- Move forward
            engine.World.Time.Current = new DateTime(1970, 1, 1);
            Assert.AreEqual(170, engine.World.Time.Iteration);
            Assert.AreEqual(170000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            //-- Move backward
            engine.World.Time.Current = new DateTime(1810, 1, 1);
            Assert.AreEqual(10, engine.World.Time.Iteration);
            Assert.AreEqual(10000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
        }

        /// <summary>
        /// Same as TestTimeTravelSimple01() but using the iteration counter
        /// </summary>
        [Test]
        public void TestIterationTravelSimple01()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            //-- Current iteration
            Assert.AreEqual(165, engine.World.Time.Iteration);
            //-- Move forward
            engine.World.Time.Iteration = 170;
            Assert.AreEqual(170, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1970, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(170000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            //-- Move backward
            engine.World.Time.Iteration = 10;
            Assert.AreEqual(10, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1810, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(10000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
        }

        [Test]
        public void TestTimeTravelSimple02()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple02.yaml");
            //-- Current time
            Assert.AreEqual(165, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1965, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(83000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(8200.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
            //-- Move forward
            engine.World.Time.Current = new DateTime(1970, 1, 1);
            Assert.AreEqual(170, engine.World.Time.Iteration);
            Assert.AreEqual(85500.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(8450.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
            //-- Move backward
            engine.World.Time.Current = new DateTime(1810, 1, 1);
            Assert.AreEqual(10, engine.World.Time.Iteration);
            Assert.AreEqual(5500.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(450.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
        }

        [Test]
        public void LoadSimple02Yaml()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple02.yaml");
            ICell cell = engine.World.Map.Cells[1, 0];
            JM2Factory jm2Factory = (JM2Factory) cell.Jm2;
            Assert.IsNotNull(jm2Factory);
        }

        [Test]
        public void RunSimple02Yaml()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple02.yaml");
            engine.World.Time.Restart();
            Assert.AreEqual(0, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1800, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("co2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
            engine.World.Time.Step();
            Assert.AreEqual(1, engine.World.Time.Iteration);
            Assert.AreEqual(new DateTime(1801, 1, 1), engine.World.Time.Current);
            Assert.AreEqual(1000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("co2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
            engine.World.Time.Step();
            Assert.AreEqual(2, engine.World.Time.Iteration);
            Assert.AreEqual(1500.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("co2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("coal"));
            Assert.AreEqual(50.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
            engine.World.Time.Step();
            Assert.AreEqual(3, engine.World.Time.Iteration);
            Assert.AreEqual(2000.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("co2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("coal"));
            Assert.AreEqual(100.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
        }

        [Test]
        public void RunDepletion01()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/depletion01.yaml");
            engine.World.Time.Restart();
            Assert.AreEqual(0, engine.World.Time.Iteration);
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("o2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[0, 0].GetStock("co2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("o2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[2, 0].GetStock("coal"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[2, 0].GetStock("o2"));
            Assert.AreEqual(0.0f, engine.World.Map.Cells[2, 0].GetStock("co2"));
            engine.World.Time.Step();
            Assert.AreEqual(1, engine.World.Time.Iteration);
            // At this point we only mine:
            Assert.AreEqual(100.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(1.0f, engine.World.Map.Cells[0, 0].Jm2.Efficiency);
            engine.World.Time.Step();
            // Now that we have some stock we can produce: 100 + 100 - 50 = 150 
            Assert.AreEqual(2, engine.World.Time.Iteration);
            Assert.AreEqual(150.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(1.0f, engine.World.Map.Cells[0, 0].Jm2.Efficiency);
            Assert.AreEqual(50.0f, engine.World.Map.Cells[2, 0].GetStock("co2"));
            Assert.AreEqual(1.0f, engine.World.Map.Cells[2, 0].Jm2.Efficiency);
            // The mine runs out of reserve and drops to 25%
            engine.World.Time.Current = new DateTime(1803, 1, 1);
            Assert.AreEqual(125.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(0.25f, engine.World.Map.Cells[0, 0].Jm2.Efficiency);
        }
    }
}