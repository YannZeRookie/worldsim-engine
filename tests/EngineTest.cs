using System;
using System.Collections.Generic;
using NUnit.Framework;
using WorldSim.API;
using WorldSim.Model;

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
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            //-- Header
            Assert.NotNull(engine.World);
            Assert.AreEqual("scenario", engine.World.Type);
            Assert.AreEqual(new DateTime(2021, 01, 02, 10, 57, 43), engine.World.ModDate);
            Assert.AreEqual("Yann Corno", engine.World.Author["name"]);
            //-- Background
            Assert.IsNotNull(engine.World.Units["mass-t"]);
            Assert.IsFalse(engine.World.Units.ContainsKey("toto"));
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var a = engine.World.Units["toto"];
            });
            Assert.IsNotNull(engine.World.Resources["coal"]);
            Assert.AreEqual("mass-t", engine.World.Resources["coal"].Unit.Id);

            var kpi = engine.World.Kpis[0];
            Assert.AreEqual("Coal Stock", kpi.Name);
            Assert.AreEqual("mass-t", kpi.Unit.Id);

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
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml", true);
            Assert.AreEqual(new DateTime(1800, 1, 1), engine.World.Time.Start);
            Assert.AreEqual(engine.World.Time.Start, engine.World.Time.Current);
        }

        /// <summary>
        ///     Test that we can read the format demo file
        /// </summary>
        [Test]
        public void LoadFormatYaml()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../../engine/doc/format.yaml");
            Assert.NotNull(engine.World);
        }

        [Test]
        public void TestKpi()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            var kpi = engine.World.Kpis[0];
            Assert.AreEqual("Coal Stock", kpi.Name);
            engine.World.Time.Restart();
            engine.World.Time.Step();
            Assert.AreEqual(1000.0f, kpi.GetValue());
        }

        [Test]
        public void RunSimple01Yaml()
        {
            var engine = new Engine();
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
            var engine = new Engine();
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
        ///     Same as TestTimeTravelSimple01() but using the iteration counter
        /// </summary>
        [Test]
        public void TestIterationTravelSimple01()
        {
            var engine = new Engine();
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
            var engine = new Engine();
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
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple02.yaml");
            var cell = engine.World.Map.Cells[1, 0];
            var jm2Factory = cell.Jm2;
            Assert.IsNotNull(jm2Factory);
            var init = jm2Factory.Init;
            var opex = (DataDictionary) init["opex"];
            Assert.IsTrue(opex.ContainsKey("coal"));
            Assert.AreEqual("500", opex["coal"].StringValue);
        }

        [Test]
        public void RunSimple02Yaml()
        {
            var engine = new Engine();
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
            var engine = new Engine();
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

        [Test]
        public void InitialStock01Yaml()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/initial_stocks.yaml", true);
            Assert.NotNull(engine.World);

            engine.World.Time.Restart();
            var cell = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(900.0f, cell.GetStock("coal"));

            engine.World.Time.Step();
            Assert.AreEqual(700.0f, cell.GetStock("coal"));
            Assert.AreEqual(100.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
        }

        [Test]
        public void TestSourcesAndSinks()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/map02.yaml", true);
            Assert.NotNull(engine.World);

            var limitedSource = engine.World.Map.Cells[0, 0];
            Assert.AreEqual("source", limitedSource.Jm2.Id);
            var unlimitedSource = engine.World.Map.Cells[1, 0];
            Assert.AreEqual("source", unlimitedSource.Jm2.Id);
            var factory = engine.World.Map.Cells[0, 1];
            Assert.AreEqual("factory", factory.Jm2.Id);
            var sink = engine.World.Map.Cells[1, 1];
            Assert.AreEqual("sink", sink.Jm2.Id);
            Assert.AreEqual(200.0f, sink.Jm2.Values["limit"].FloatValue);

            engine.World.Time.Step();

            Assert.AreEqual(100.0f, limitedSource.GetStock("coal"));
            Assert.AreEqual(300.0f, unlimitedSource.GetStock("o2"));
            Assert.AreEqual(0.0f, factory.GetStock("co2"));
            Assert.AreEqual(0.0f, ((DataDictionary) factory.Jm2.Values["produced"])["co2"].FloatValue);

            engine.World.Time.Step();
            Assert.AreEqual(2.0 * 100.0f - 50.0f, limitedSource.GetStock("coal"));
            Assert.AreEqual(2.0 * 300.0f - 100.0f, unlimitedSource.GetStock("o2"));
            Assert.AreEqual(50.0f, factory.GetStock("co2"));
            Assert.AreEqual(50.0f, ((DataDictionary) factory.Jm2.Values["produced"])["co2"].FloatValue);
            Assert.AreEqual(0.0f, sink.Jm2.Values["consumed"].FloatValue);
            Assert.AreEqual(200.0f, sink.Jm2.Values["limit"].FloatValue);

            engine.World.Time.Step();
            Assert.AreEqual(2 * 50.0f - 25.0f, factory.GetStock("co2"));
            Assert.AreEqual(50.0f, ((DataDictionary) factory.Jm2.Values["produced"])["co2"].FloatValue);
            Assert.AreEqual(25.0f, sink.Jm2.Values["consumed"].FloatValue);
            Assert.AreEqual(175.0f, sink.Jm2.Values["limit"].FloatValue);
        }

        [Test]
        public void TestSimpleFactory()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/factory01.yaml", true);
            engine.World.Time.Restart();
            engine.World.Time.Step();
            Assert.AreEqual(1.0f, engine.World.Map.Cells[1, 0].Jm2.Efficiency);
            Assert.AreEqual(50.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(900.0f, engine.World.Map.Cells[0, 0].GetStock("o2"));
            Assert.AreEqual(150.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
            Assert.AreEqual(150.0f,
                ((DataDictionary) engine.World.Map.Cells[1, 0].Jm2.Values["produced"])["co2"].FloatValue);
        }

        [Test]
        public void TestSimpleSink()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink01.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var emptyCell = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell.Jm2);
            var sink = engine.World.Map.Cells[1, 0];
            Assert.AreEqual("sink", sink.Jm2.Id);
            Assert.AreEqual("coal", sink.Jm2.Values["resource_id"].StringValue);
            Assert.AreEqual(100.0f, sink.Jm2.Values["consumption"].FloatValue);
            Assert.AreEqual(0.0f, sink.Jm2.Values["consumed"].FloatValue);

            engine.World.Time.Step();

            Assert.AreEqual(900.0f, emptyCell.GetStock("coal"));
            Assert.AreEqual(100.0f, sink.Jm2.Values["consumed"].FloatValue);
        }

        [Test]
        public void TestSimpleSinkTwoFullStocks()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink02.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var emptyCell1 = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell1.Jm2);
            var emptyCell2 = engine.World.Map.Cells[1, 0];
            Assert.AreEqual(null, emptyCell2.Jm2);
            var sink = engine.World.Map.Cells[2, 0];
            Assert.AreEqual("sink", sink.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(10.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(15.0f, emptyCell2.GetStock("coal"));
        }

        [Test]
        public void TestSimpleSinkTwoShortStocks()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink02B.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var emptyCell1 = engine.World.Map.Cells[0, 0];
            var emptyCell2 = engine.World.Map.Cells[1, 0];
            var sink1 = engine.World.Map.Cells[2, 0];

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(0.0f, emptyCell2.GetStock("coal"));
            Assert.AreEqual(0.9f, sink1.Jm2.Efficiency);
        }

        [Test]
        public void TestSimpleTwoSinks()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink03.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var emptyCell = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell.Jm2);
            var sink1 = engine.World.Map.Cells[1, 0];
            Assert.AreEqual("sink", sink1.Jm2.Id);
            var sink2 = engine.World.Map.Cells[2, 0];
            Assert.AreEqual("sink", sink2.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(0.0, Math.Round(emptyCell.GetStock("coal")));
            Assert.AreEqual(0.8, Math.Round((double) sink1.Jm2.Efficiency, 1));
            Assert.AreEqual(0.8, Math.Round((double) sink2.Jm2.Efficiency, 1));
        }

        [Test]
        public void TwoStocksTwoSinks()
        {
            float[] demands = {100, 150};
            float[] stocks = {300, 200};
            float[,] cluster = {{1, 1}, {1, 1}};
            var allocationTable = Allocation.SolomonSpread(cluster, stocks, demands);
            Assert.AreEqual(60.0f, Math.Round(allocationTable[0, 0]));
            Assert.AreEqual(40.0f, Math.Round(allocationTable[0, 1]));
            Assert.AreEqual(90.0f, Math.Round(allocationTable[1, 0]));
            Assert.AreEqual(60.0f, Math.Round(allocationTable[1, 1]));
        }

        [Test]
        public void ThreeStocksOneSharedTwoSinks()
        {
            float[] demands = {100, 150};
            float[] stocks = {300, 200, 100};
            float[,] cluster = {{1, 1, 0}, {0, 1, 1}};
            var allocationTable = Allocation.SolomonSpread(cluster, stocks, demands);
            Assert.AreEqual(79.0f, Math.Round(allocationTable[0, 0]));
            Assert.AreEqual(21.0f, Math.Round(allocationTable[0, 1]));
            Assert.AreEqual(0.0f, Math.Round(allocationTable[0, 2]));
            Assert.AreEqual(0.0f, Math.Round(allocationTable[1, 0]));
            Assert.AreEqual(82.0f, Math.Round(allocationTable[1, 1]));
            Assert.AreEqual(68.0f, Math.Round(allocationTable[1, 2]));
        }

        [Test]
        public void ThreeShortStocksOneSharedTwoSinks()
        {
            float[] demands = {200, 300};
            float[] stocks = {100, 200, 150};
            float[,] cluster = {{1, 1, 0}, {0, 1, 1}};
            var allocationTable = Allocation.SolomonSpread(cluster, stocks, demands);
            Assert.AreEqual(100.0f, Math.Round(allocationTable[0, 0]));
            Assert.AreEqual(80.0f, Math.Round(allocationTable[0, 1]));
            Assert.AreEqual(0.0f, Math.Round(allocationTable[0, 2]));
            Assert.AreEqual(0.0f, Math.Round(allocationTable[1, 0]));
            Assert.AreEqual(120.0f, Math.Round(allocationTable[1, 1]));
            Assert.AreEqual(150.0f, Math.Round(allocationTable[1, 2]));
        }

        [Test]
        public void TestSimpleTwoShortStocksTwoSinks()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink04.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var emptyCell1 = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell1.Jm2);
            var emptyCell2 = engine.World.Map.Cells[1, 0];
            Assert.AreEqual(null, emptyCell2.Jm2);
            var sink1 = engine.World.Map.Cells[2, 0];
            Assert.AreEqual("sink", sink1.Jm2.Id);
            var sink2 = engine.World.Map.Cells[3, 0];
            Assert.AreEqual("sink", sink2.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(0.0f, emptyCell2.GetStock("coal"));
            Assert.AreEqual(0.8f, sink1.Jm2.Efficiency);
            Assert.AreEqual(0.8f, sink2.Jm2.Efficiency);
        }

        /// <summary>
        ///     Here we have plenty of initial stock
        /// </summary>
        [Test]
        public void TestSimpleTwoFullStocksTwoSinks()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink05.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var emptyCell1 = engine.World.Map.Cells[0, 0];
            var emptyCell2 = engine.World.Map.Cells[1, 0];
            var sink1 = engine.World.Map.Cells[2, 0];
            var sink2 = engine.World.Map.Cells[3, 0];
            Assert.AreEqual(300.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(200.0f, emptyCell2.GetStock("coal"));

            engine.World.Time.Step();

            Assert.AreEqual(250.0f, engine.World.Map.TotalStock("coal"));
            Assert.AreEqual(150.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(100.0f, emptyCell2.GetStock("coal"));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);
            Assert.AreEqual(1.0f, sink2.Jm2.Efficiency);
        }

        [Test]
        public void TestLocalThreeStocksOneSharedTwoSinks()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink06.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[0, 0];
            var stock2 = engine.World.Map.Cells[1, 0];
            var stock3 = engine.World.Map.Cells[2, 0];
            var sink1 = engine.World.Map.Cells[0, 1];
            var sink2 = engine.World.Map.Cells[2, 1];
            Assert.AreEqual(300.0f, stock1.GetStock("coal"));
            Assert.AreEqual(200.0f, stock2.GetStock("coal"));
            Assert.AreEqual(100.0f, stock3.GetStock("coal"));

            engine.World.Time.Step();

            Assert.AreEqual(350.0f, engine.World.Map.TotalStock("coal"));
            Assert.AreEqual(221.0f, Math.Round(stock1.GetStock("coal")));
            Assert.AreEqual(97.0f, Math.Round(stock2.GetStock("coal")));
            Assert.AreEqual(32.0f, Math.Round(stock3.GetStock("coal")));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);
            Assert.AreEqual(1.0f, sink2.Jm2.Efficiency);
        }

        [Test]
        public void TestLocalTwoCellsDistance()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink07.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[0, 0];
            var stock2 = engine.World.Map.Cells[2, 0];
            var stock3 = engine.World.Map.Cells[4, 0];
            var sink1 = engine.World.Map.Cells[0, 1];
            var sink2 = engine.World.Map.Cells[4, 1];
            Assert.AreEqual(300.0f, stock1.GetStock("coal"));
            Assert.AreEqual(200.0f, stock2.GetStock("coal"));
            Assert.AreEqual(100.0f, stock3.GetStock("coal"));

            engine.World.Time.Step();

            Assert.AreEqual(350.0f, engine.World.Map.TotalStock("coal"));
            Assert.AreEqual(221.0f, Math.Round(stock1.GetStock("coal")));
            Assert.AreEqual(97.0f, Math.Round(stock2.GetStock("coal")));
            Assert.AreEqual(32.0f, Math.Round(stock3.GetStock("coal")));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);
            Assert.AreEqual(1.0f, sink2.Jm2.Efficiency);
        }

        /// <summary>
        ///     Test of the attenuation algorithm
        /// </summary>
        [Test]
        public void TestSinksAttenuation()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink08.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[0, 0];
            var stock2 = engine.World.Map.Cells[2, 0];
            var stock3 = engine.World.Map.Cells[3, 0];
            var sink1 = engine.World.Map.Cells[0, 1];
            var sink2 = engine.World.Map.Cells[3, 1];
            Assert.AreEqual(300.0f, stock1.GetStock("coal"));
            Assert.AreEqual(200.0f, stock2.GetStock("coal"));
            Assert.AreEqual(100.0f, stock3.GetStock("coal"));

            engine.World.Time.Step();

            Assert.AreEqual(350.0f, Math.Round(engine.World.Map.TotalStock("coal")));
            Assert.AreEqual(199.0f, Math.Round(stock1.GetStock("coal")));
            Assert.AreEqual(102.0f, Math.Round(stock2.GetStock("coal")));
            Assert.AreEqual(49.0f, Math.Round(stock3.GetStock("coal")));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);
            Assert.AreEqual(1.0f, sink2.Jm2.Efficiency);
        }

        /// <summary>
        ///     Test of the First distribution algorithm
        /// </summary>
        [Test]
        public void TestSinksFirst()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink09.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[0, 0];
            var stock2 = engine.World.Map.Cells[3, 0];
            var stock3 = engine.World.Map.Cells[6, 0];
            var sink1 = engine.World.Map.Cells[2, 0];
            var sink2 = engine.World.Map.Cells[5, 0];
            Assert.AreEqual(200.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock2.GetStock("coal"));
            Assert.AreEqual(100.0f, stock3.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);
            Assert.AreEqual("sink", sink2.Jm2.Id);

            foreach (var cell in engine.World.Map.Cells) ((Cell) cell).StepPrepare((Time) engine.World.Time);

            var allocator =
                Allocator.Allocate((Time) engine.World.Time, engine.World.Resources, (Map) engine.World.Map);
            var allocation = allocator.Allocations["coal"];
            // D1 demand:
            Assert.AreEqual(100.0f, allocation.AllocationTable[0, 0]);
            Assert.AreEqual(100.0f, allocation.AllocationTable[0, 1]);
            Assert.AreEqual(0.0f, allocation.AllocationTable[0, 2]);
            // D2 demand:
            Assert.AreEqual(50.0f, allocation.AllocationTable[1, 0]);
            Assert.AreEqual(0.0f, allocation.AllocationTable[1, 1]);
            Assert.AreEqual(100.0f, allocation.AllocationTable[1, 2]);
        }

        /// <summary>
        ///     Test of the Philippe's Paradox
        /// </summary>
        [Test]
        public void TestPhilippeParadox()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink10.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[0, 0];
            var stock2 = engine.World.Map.Cells[3, 0];
            var sink1 = engine.World.Map.Cells[2, 0];
            var sink2 = engine.World.Map.Cells[5, 0];
            Assert.AreEqual(200.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock2.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);
            Assert.AreEqual("sink", sink2.Jm2.Id);

            foreach (var cell in engine.World.Map.Cells) ((Cell) cell).StepPrepare((Time) engine.World.Time);

            var allocator =
                Allocator.Allocate((Time) engine.World.Time, engine.World.Resources, (Map) engine.World.Map);
            var allocation = allocator.Allocations["coal"];
            // D1 demand:
            Assert.AreEqual(0.0f, allocation.AllocationTable[0, 0]);
            Assert.AreEqual(100.0f, allocation.AllocationTable[0, 1]);
            // D2 demand:
            Assert.AreEqual(100.0f, allocation.AllocationTable[1, 0]);
            Assert.AreEqual(0.0f, allocation.AllocationTable[1, 1]);
        }

        /// <summary>
        ///     First distribution, split case
        /// </summary>
        [Test]
        public void TestSplitFirst()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink11.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[1, 0];
            var sink1 = engine.World.Map.Cells[0, 0];
            var sink2 = engine.World.Map.Cells[2, 0];
            Assert.AreEqual(300.0f, stock1.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);
            Assert.AreEqual("sink", sink2.Jm2.Id);

            foreach (var cell in engine.World.Map.Cells) ((Cell) cell).StepPrepare((Time) engine.World.Time);

            var allocator =
                Allocator.Allocate((Time) engine.World.Time, engine.World.Resources, (Map) engine.World.Map);
            var allocation = allocator.Allocations["coal"];
            // D1 demand:
            Assert.AreEqual(100.0f, allocation.AllocationTable[0, 0]);
            Assert.AreEqual(200.0f, allocation.AllocationTable[1, 0]);

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, stock1.GetStock("coal"));
            Assert.AreEqual(33, Math.Round((double) sink1.Jm2.Efficiency * 100.0));
            Assert.AreEqual(33, Math.Round((double) sink2.Jm2.Efficiency * 100.0));
        }

        /// <summary>
        ///     First distribution, sophisticated 2D case
        /// </summary>
        [Test]
        public void TestSophisticatedFirst()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink14.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[2, 1];
            var stock2 = engine.World.Map.Cells[0, 2];
            var stock3 = engine.World.Map.Cells[3, 3];
            var sink1 = engine.World.Map.Cells[3, 0];
            var sink2 = engine.World.Map.Cells[0, 3];
            Assert.AreEqual(200.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock2.GetStock("coal"));
            Assert.AreEqual(300.0f, stock3.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);
            Assert.AreEqual("sink", sink2.Jm2.Id);

            foreach (var cell in engine.World.Map.Cells) ((Cell) cell).StepPrepare((Time) engine.World.Time);

            var allocator =
                Allocator.Allocate((Time) engine.World.Time, engine.World.Resources, (Map) engine.World.Map);
            var allocation = allocator.Allocations["coal"];
            // D2 demand:
            Assert.AreEqual(100.0f, allocation.AllocationTable[0, 0]); // S2
            Assert.AreEqual(0.0f, allocation.AllocationTable[0, 1]); // S1
            Assert.AreEqual(100.0f, allocation.AllocationTable[0, 2]); // S3
            // D1 demand:
            Assert.AreEqual(0.0f, allocation.AllocationTable[1, 0]); // S2
            Assert.AreEqual(200.0f, allocation.AllocationTable[1, 1]); // S1
            Assert.AreEqual(100.0f, allocation.AllocationTable[1, 2]); // S3

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, stock1.GetStock("coal"));
            Assert.AreEqual(0.0f, stock2.GetStock("coal"));
            Assert.AreEqual(100.0f, stock3.GetStock("coal"));
            Assert.AreEqual(100, Math.Round((double) sink1.Jm2.Efficiency * 100.0));
            Assert.AreEqual(100, Math.Round((double) sink2.Jm2.Efficiency * 100.0));
        }

        /// <summary>
        ///     First Cascade test
        /// </summary>
        [Test]
        public void TestFirstCascade()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink12.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[1, 0];
            var stock2 = engine.World.Map.Cells[2, 0];
            var sink1 = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(100.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock2.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(25.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock2.GetStock("coal"));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, stock1.GetStock("coal"));
            Assert.AreEqual(50.0f, stock2.GetStock("coal"));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, stock1.GetStock("coal"));
            Assert.AreEqual(0.0f, stock2.GetStock("coal"));
            Assert.AreEqual((float) (50.0 / 75.0), sink1.Jm2.Efficiency);
        }

        /// <summary>
        ///     Test of the Nearest distribution algorithm
        ///     Simple case, one sink is further but it still gets 50% of the source
        /// </summary>
        [Test]
        public void TestSinksNearest()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink15.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[1, 0];
            var sink1 = engine.World.Map.Cells[0, 0];
            var sink2 = engine.World.Map.Cells[3, 0];
            Assert.AreEqual(100.0f, stock1.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);
            Assert.AreEqual("sink", sink2.Jm2.Id);

            foreach (var cell in engine.World.Map.Cells) ((Cell) cell).StepPrepare((Time) engine.World.Time);

            var allocator =
                Allocator.Allocate((Time) engine.World.Time, engine.World.Resources, (Map) engine.World.Map);
            var allocation = allocator.Allocations["coal"];
            // D1 demand:
            Assert.AreEqual(50.0f, allocation.AllocationTable[0, 0]);
            // D2 demand:
            Assert.AreEqual(50.0f, allocation.AllocationTable[1, 0]);
        }

        /// <summary>
        ///     Test of the Nearest distribution algorithm
        ///     Unfair case: one Sink grabs a shared source
        /// </summary>
        [Test]
        public void TestSinksUnfairNearest()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink16.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[0, 0];
            var stock2 = engine.World.Map.Cells[2, 0];
            var stock3 = engine.World.Map.Cells[7, 0];
            var sink1 = engine.World.Map.Cells[1, 0];
            var sink2 = engine.World.Map.Cells[4, 0];
            Assert.AreEqual(100.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock1.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);
            Assert.AreEqual("sink", sink2.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(33.0f, Math.Round(stock1.GetStock("coal")));
            Assert.AreEqual(17.0f, Math.Round(stock2.GetStock("coal")));
            Assert.AreEqual(50.0f, Math.Round(stock3.GetStock("coal")));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);
            Assert.AreEqual(1.0f, sink2.Jm2.Efficiency);
        }

        /// <summary>
        ///     Radius Distribution test
        /// </summary>
        [Test]
        public void TestRadius()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink13.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            var stock1 = engine.World.Map.Cells[0, 0];
            var stock2 = engine.World.Map.Cells[1, 0];
            var stock3 = engine.World.Map.Cells[4, 0];
            var sink1 = engine.World.Map.Cells[2, 0];
            Assert.AreEqual(100.0f, stock1.GetStock("coal"));
            Assert.AreEqual(100.0f, stock2.GetStock("coal"));
            Assert.AreEqual(200.0f, stock3.GetStock("coal"));
            Assert.AreEqual("sink", sink1.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(33.0f, Math.Round(stock1.GetStock("coal")));
            Assert.AreEqual(100.0f, stock2.GetStock("coal")); // No change
            Assert.AreEqual(67.0f, Math.Round(stock3.GetStock("coal")));
            Assert.AreEqual(1.0f, sink1.Jm2.Efficiency);
        }

        [Test]
        public void CellsShouldBeAbleToHaveNoJM2()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/empty_cells.yaml");
            engine.World.Time.Restart();
            engine.World.Time.Step();
            engine.World.Time.Step();
        }

        [Test]
        public void AKpiCanHaveNoUnit()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/nounit_kpi.yaml", true);
            var kpi = engine.World.Kpis[0];
            Assert.IsNull(kpi.Unit);
            engine.World.Time.Restart();
            engine.World.Time.Step();
            engine.World.Time.Step();
            Assert.AreEqual(2.0f, kpi.GetValue());
        }

        [Test]
        public void AResourceCanHaveNoUnit()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/nounit_resource.yaml", true);
            var kpi = engine.World.Kpis[0];
            var cell = engine.World.Map.Cells[0, 0];
            Assert.IsNull(kpi.Unit);
            engine.World.Time.Restart();
            Assert.AreEqual(0.0f, cell.GetStock("tech"));
            engine.World.Time.Step();
            Assert.AreEqual(1.0f, cell.GetStock("tech"));
            engine.World.Time.Step();
            Assert.AreEqual(2.0f, kpi.GetValue());
            Assert.AreEqual(2.0f, cell.GetStock("tech"));
        }

        [Test]
        public void TestSimpleSource01()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/source01.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();
            var sourceCell = engine.World.Map.Cells[0, 0];
            var jm2 = sourceCell.Jm2;

            Assert.AreEqual(0.0f, sourceCell.GetStock("stuff"));
            Assert.AreEqual("stuff", jm2.Values["resource_id"].StringValue);

            engine.World.Time.Step();
            Assert.AreEqual(100.0f, sourceCell.GetStock("stuff"));
            Assert.AreEqual(900.0f, jm2.Values["reserve"].FloatValue);
            Assert.AreEqual(100.0f, jm2.Values["produced"].FloatValue);

            engine.World.Time.Step();
            Assert.AreEqual(200.0f, sourceCell.GetStock("stuff"));
            Assert.AreEqual(800.0f, jm2.Values["reserve"].FloatValue);
            Assert.AreEqual(100.0f, jm2.Values["produced"].FloatValue);

            engine.World.Time.Iteration = 9;
            Assert.AreEqual(100.0f, jm2.Values["reserve"].FloatValue);
            Assert.AreEqual(100.0f, jm2.Values["produced"].FloatValue);

            engine.World.Time.Step();
            Assert.AreEqual(1000.0f, sourceCell.GetStock("stuff"));
            Assert.AreEqual(0.0f, jm2.Values["reserve"].FloatValue);
            Assert.AreEqual(100.0f, jm2.Values["produced"].FloatValue);

            // Source is now exhausted
            engine.World.Time.Step();
            Assert.AreEqual(1000.0f, sourceCell.GetStock("stuff"));
            Assert.AreEqual(0.0f, jm2.Values["reserve"].FloatValue);
            Assert.AreEqual(0.0f, jm2.Values["produced"].FloatValue);
        }

        [Test]
        public void TestVolatileStock()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/volatile01.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            Assert.AreEqual(100.0f, engine.World.Map.TotalStock("stuff"));

            // Volatile resources don't add up from one iteration to the next
            engine.World.Time.Step();
            Assert.AreEqual(100.0f, engine.World.Map.TotalStock("stuff"));

            engine.World.Time.Step();
            Assert.AreEqual(100.0f, engine.World.Map.TotalStock("stuff"));
        }

        [Test]
        public void TestLastIteration()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml", true);
            Assert.AreEqual(400, engine.World.Time.LastIteration());
        }

        [Test]
        public void TestLastIterationStepValueRound()
        {
            var world = new World();
            world.Time.Start = new DateTime(1900, 1, 1);
            world.Time.End = new DateTime(2100, 1, 1);
            world.Time.StepValue = 2;
            Assert.AreEqual(100, world.Time.LastIteration());
        }

        [Test]
        public void TestLastIterationStepValueOvershot()
        {
            var world = new World();
            world.Time.Start = new DateTime(1900, 1, 1);
            world.Time.End = new DateTime(1910, 1, 1);
            world.Time.StepValue = 3;
            Assert.AreEqual(4, world.Time.LastIteration());
            world.Time.Current = world.Time.End;
            Assert.AreEqual(4, world.Time.Iteration);
        }

        [Test]
        public void TestLastIterationMonths()
        {
            var world = new World();
            world.Time.Start = new DateTime(1900, 1, 1);
            world.Time.End = new DateTime(1901, 6, 1);
            world.Time.StepUnit = TimeStep.month;
            Assert.AreEqual(17, world.Time.LastIteration());
        }

        [Test]
        public void TestLastIterationMonthsStepOvershoot()
        {
            var world = new World();
            world.Time.Start = new DateTime(1900, 1, 1);
            world.Time.End = new DateTime(1901, 1, 1);
            world.Time.StepUnit = TimeStep.month;
            world.Time.StepValue = 5;
            Assert.AreEqual(3, world.Time.LastIteration());
        }

        [Test]
        public void TestLastIterationDays()
        {
            var world = new World();
            world.Time.Start = new DateTime(1965, 1, 1);
            world.Time.End = new DateTime(1965, 12, 31);
            world.Time.StepUnit = TimeStep.day;
            Assert.AreEqual(364, world.Time.LastIteration());
        }

        [Test]
        public void TestLastIterationDaysLeapYear1()
        {
            var world = new World();
            world.Time.Start = new DateTime(1904, 1, 1);
            world.Time.End = new DateTime(1904, 12, 31);
            world.Time.StepUnit = TimeStep.day;
            Assert.AreEqual(365, world.Time.LastIteration()); // 1904 is a Leap Year
        }

        [Test]
        public void TestLastIterationDaysLeapYear2()
        {
            var world = new World();
            world.Time.Start = new DateTime(1900, 1, 1);
            world.Time.End = new DateTime(1900, 12, 31);
            world.Time.StepUnit = TimeStep.day;
            Assert.AreEqual(364, world.Time.LastIteration());
            // 1900 is NOT a Leap Year: it is divisible by 4 but it is not divisible by 400 although it is divisible by 100
            // Verification:
            world.Time.Current = world.Time.End;
            Assert.AreEqual(364, world.Time.Iteration);
        }

        /// <summary>
        /// Various tests of IDataNode traversing and accessing
        /// </summary>
        [Test]
        public void TestDataNodeAccessors()
        {
            // Build the data set:
            DataDictionary root = new DataDictionary()
            {
                {"res", "coal"},
                {"qty", 123.45f},
                {"qty2", "42"},
                {
                    "ceo", new DataDictionary()
                    {
                        {"first_name", "John"},
                        {"last_name", "Doe"},
                        {"age", 56.0f}
                    }
                }
            };
            // Let's manipulate it in an abstract way:
            IDataNode rootNode = root;
            Assert.AreEqual("coal", rootNode["res"].StringValue);
            Assert.AreEqual(123.45f, rootNode["qty"].FloatValue);
            Assert.AreEqual("42", rootNode["qty2"].StringValue);
            Assert.AreEqual(42.0f, rootNode["qty2"].FloatValue);
            Assert.IsInstanceOf<DataValue>(rootNode["qty2"]);
            DataValue d = (DataValue) rootNode["qty2"];
            Assert.AreEqual(ValueKind.Float, d.Kind);

            Assert.AreEqual("Doe", rootNode["ceo"]["last_name"].StringValue);
            Assert.AreEqual("Doe", rootNode["ceo"][1].StringValue); // Works as index too (but risky)
            rootNode["ceo"]["first_name"].StringValue = "Bruce";
            Assert.AreEqual("Bruce", rootNode["ceo"]["first_name"].StringValue);

            // Adding data (here a list of strings)
            DataList list = new DataList() {"a", "b", "c"};
            rootNode["alphabet"] = list;
            Assert.AreEqual(3, rootNode["alphabet"].Count);
            Assert.AreEqual("b", rootNode["alphabet"][1].StringValue);
            Assert.AreEqual("b", rootNode["alphabet"]["1"].StringValue); // Works as a key too
            rootNode["alphabet"][1].StringValue = "bb";
            Assert.AreEqual("bb", rootNode["alphabet"][1].StringValue);
            
            // Iterators: iterating on DataDictionary is natural
            foreach (KeyValuePair<string, IDataNode> n1 in rootNode)
            {
                Console.WriteLine(n1);
            }
            // Iterators: you can also iterate on DataList. In this case, the key is the list index as a string
            foreach (KeyValuePair<string, IDataNode> n2 in rootNode["alphabet"])
            {
                Console.WriteLine(n2);
            }
        }

        /// <summary>
        /// IDataNode tests with a real WorldSim file
        /// </summary>
        [Test]
        public void TestDataNodeWithMap()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/map02.yaml", true);
            Assert.NotNull(engine.World);

            var limitedSource = engine.World.Map.Cells[0, 0];
            Assert.AreEqual("source", limitedSource.Jm2.Id);
            var unlimitedSource = engine.World.Map.Cells[1, 0];
            Assert.AreEqual("source", unlimitedSource.Jm2.Id);
            var factory = engine.World.Map.Cells[0, 1];
            Assert.AreEqual("factory", factory.Jm2.Id);
            var sink = engine.World.Map.Cells[1, 1];
            Assert.AreEqual("sink", sink.Jm2.Id);

            IDataNode vals = factory.Jm2.Values;
            Assert.AreEqual(3, vals.Count);
            // Accessing using indexes
            var opex1 = vals[0];
            Assert.AreEqual(2, opex1.Count);
            // Accessing using keys
            var opex2 = vals["opex"];
            Assert.AreEqual(2, opex1.Count);
            // Cascade access
            Assert.AreEqual(50.0, vals["opex"]["coal"].FloatValue);
            // Non-existent Key
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var v = vals["opex"]["stuff"].FloatValue;
            });
            // Non-existent Index
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var v = vals[666].FloatValue;
            });
        }

        [Test]
        public void TestChangeJM2Init()
        {
            var engine = new Engine();
            engine.LoadYaml("../../../fixtures/map02.yaml", true);
            Assert.NotNull(engine.World);
            
            var limitedSource = engine.World.Map.Cells[0, 0];
            Assert.AreEqual("source", limitedSource.Jm2.Id);
            var init = limitedSource.Jm2.Init;
            Assert.AreEqual(100.0f, limitedSource.Jm2.Init["production"].FloatValue);
            limitedSource.Jm2.Init["production"].FloatValue = 200.0f;
            Assert.AreEqual(200.0f, limitedSource.Jm2.Init["production"].FloatValue);
            engine.World.Time.Restart();
            Assert.AreEqual(0.0f, limitedSource.GetStock("coal"));
            engine.World.Time.Step();
            Assert.AreEqual(200.0f, limitedSource.GetStock("coal"));
            Assert.AreEqual(25.0f, limitedSource.Jm2.Values["reserve"].FloatValue);
        }
    }
}
