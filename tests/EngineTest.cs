using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using WorldSim.API;

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
            Assert.AreEqual("mass-t", engine.World.Resources["coal"].Unit.Id);

            IKpi kpi = engine.World.Kpis[0];
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
        public void TestKpi()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/simple01.yaml");
            IKpi kpi = engine.World.Kpis[0];
            Assert.AreEqual("Coal Stock", kpi.Name);
            engine.World.Time.Restart();
            engine.World.Time.Step();
            Assert.AreEqual(1000.0f, kpi.GetValue(engine.World));
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

        [Test]
        public void InitialStock01Yaml()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/initial_stocks.yaml", true);
            Assert.NotNull(engine.World);

            engine.World.Restart();
            ICell cell = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(900.0f, cell.GetStock("coal"));

            engine.World.Time.Step();
            Assert.AreEqual(700.0f, cell.GetStock("coal"));
            Assert.AreEqual(100.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
        }

        [Test]
        public void TestSourcesAndSinks()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/map02.yaml", true);
            Assert.NotNull(engine.World);

            ICell limitedSource = engine.World.Map.Cells[0, 0];
            Assert.AreEqual("source", limitedSource.Jm2.Id);
            ICell unlimitedSource = engine.World.Map.Cells[1, 0];
            Assert.AreEqual("source", unlimitedSource.Jm2.Id);
            ICell factory = engine.World.Map.Cells[0, 1];
            Assert.AreEqual("factory", factory.Jm2.Id);
            ICell sink = engine.World.Map.Cells[1, 1];
            Assert.AreEqual("sink", sink.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(100.0f, limitedSource.GetStock("coal"));
            Assert.AreEqual(300.0f, unlimitedSource.GetStock("o2"));
            Assert.AreEqual(0.0f, factory.GetStock("co2"));

            engine.World.Time.Step();
            Assert.AreEqual(2.0 * 100.0f - 50.0f, limitedSource.GetStock("coal"));
            Assert.AreEqual(2.0 * 300.0f - 100.0f, unlimitedSource.GetStock("o2"));
            Assert.AreEqual(50.0f, factory.GetStock("co2"));

            engine.World.Time.Step();
            Assert.AreEqual(2 * 50.0f - 25.0f, factory.GetStock("co2"));
        }

        [Test]
        public void TestSimpleFactory()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/factory01.yaml", true);
            engine.World.Time.Restart();
            engine.World.Time.Step();
            Assert.AreEqual(1.0f, engine.World.Map.Cells[1, 0].Jm2.Efficiency);
            Assert.AreEqual(50.0f, engine.World.Map.Cells[0, 0].GetStock("coal"));
            Assert.AreEqual(900.0f, engine.World.Map.Cells[0, 0].GetStock("o2"));
            Assert.AreEqual(150.0f, engine.World.Map.Cells[1, 0].GetStock("co2"));
        }

        [Test]
        public void TestSimpleSink()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink01.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            ICell emptyCell = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell.Jm2);
            ICell sink = engine.World.Map.Cells[1, 0];
            Assert.AreEqual("sink", sink.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(900.0f, emptyCell.GetStock("coal"));
        }

        [Test]
        public void TestSimpleSinkTwoFullStocks()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink02.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            ICell emptyCell1 = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell1.Jm2);
            ICell emptyCell2 = engine.World.Map.Cells[1, 0];
            Assert.AreEqual(null, emptyCell2.Jm2);
            ICell sink = engine.World.Map.Cells[2, 0];
            Assert.AreEqual("sink", sink.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(10.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(15.0f, emptyCell2.GetStock("coal"));
        }

        [Test]
        public void TestSimpleSinkTwoShortStocks()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink02B.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            ICell emptyCell1 = engine.World.Map.Cells[0, 0];
            ICell emptyCell2 = engine.World.Map.Cells[1, 0];
            ICell sink1 = engine.World.Map.Cells[2, 0];

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(0.0f, emptyCell2.GetStock("coal"));
            Assert.AreEqual(0.9f, sink1.Jm2.Efficiency);
        }

        [Test]
        public void TestSimpleTwoSinks()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink03.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            ICell emptyCell = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell.Jm2);
            ICell sink1 = engine.World.Map.Cells[1, 0];
            Assert.AreEqual("sink", sink1.Jm2.Id);
            ICell sink2 = engine.World.Map.Cells[2, 0];
            Assert.AreEqual("sink", sink2.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, emptyCell.GetStock("coal"));
            Assert.AreEqual(0.8f, sink1.Jm2.Efficiency);
            Assert.AreEqual(0.8f, sink2.Jm2.Efficiency);
        }

        [Test]
        public void TestSimpleTwoShortStocksTwoSinks()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink04.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            ICell emptyCell1 = engine.World.Map.Cells[0, 0];
            Assert.AreEqual(null, emptyCell1.Jm2);
            ICell emptyCell2 = engine.World.Map.Cells[1, 0];
            Assert.AreEqual(null, emptyCell2.Jm2);
            ICell sink1 = engine.World.Map.Cells[2, 0];
            Assert.AreEqual("sink", sink1.Jm2.Id);
            ICell sink2 = engine.World.Map.Cells[3, 0];
            Assert.AreEqual("sink", sink2.Jm2.Id);

            engine.World.Time.Step();

            Assert.AreEqual(0.0f, emptyCell1.GetStock("coal"));
            Assert.AreEqual(0.0f, emptyCell2.GetStock("coal"));
            Assert.AreEqual(0.8f, sink1.Jm2.Efficiency);
            Assert.AreEqual(0.8f, sink2.Jm2.Efficiency);
        }
        
        /// <summary>
        /// Here we have plenty of initial stock
        /// </summary>
        [Test]
        public void TestSimpleTwoFullStocksTwoSinks()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/sink05.yaml", true);
            Assert.NotNull(engine.World);
            engine.World.Time.Restart();

            ICell emptyCell1 = engine.World.Map.Cells[0, 0];
            ICell emptyCell2 = engine.World.Map.Cells[1, 0];
            ICell sink1 = engine.World.Map.Cells[2, 0];
            ICell sink2 = engine.World.Map.Cells[3, 0];
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
        public void CellsShouldBeAbleToHaveNoJM2()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/empty_cells.yaml");
            engine.World.Time.Restart();
            engine.World.Time.Step();
            engine.World.Time.Step();
        }

        [Test]
        public void AKpiCanHaveNoUnit()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/nounit_kpi.yaml", true);
            IKpi kpi = engine.World.Kpis[0];
            Assert.IsNull(kpi.Unit);
            engine.World.Time.Restart();
            engine.World.Time.Step();
            engine.World.Time.Step();
            Assert.AreEqual(2.0f, kpi.GetValue(engine.World));
        }

        [Test]
        public void AResourceCanHaveNoUnit()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/nounit_resource.yaml", true);
            IKpi kpi = engine.World.Kpis[0];
            ICell cell = engine.World.Map.Cells[0, 0];
            Assert.IsNull(kpi.Unit);
            engine.World.Time.Restart();
            Assert.AreEqual(0.0f, cell.GetStock("tech"));
            engine.World.Time.Step();
            Assert.AreEqual(1.0f, cell.GetStock("tech"));
            engine.World.Time.Step();
            Assert.AreEqual(2.0f, kpi.GetValue(engine.World));
            Assert.AreEqual(2.0f, cell.GetStock("tech"));
        }

        [Test]
        public void IterateOnCells()
        {
            Engine engine = new Engine();
            engine.LoadYaml("../../../fixtures/map02.yaml", true);
            Assert.NotNull(engine.World);

            EnumerateCells(engine.World.Map.Cells.GetEnumerator());
            
            Cell cell1 = new Cell(0, 99, engine.World.Resources);
            Cell cell2 = new Cell(1, 99, engine.World.Resources);
            List<ICell> list = new List<ICell>() {cell1, cell2};
            EnumerateCells(list.GetEnumerator());
            
        }

        private void EnumerateCells(IEnumerator it)
        {
            while (it.MoveNext())
            {
                ICell cell = (ICell) it.Current;
                Console.WriteLine(cell.X + "-" + cell.Y);
            }
        }
    }
}
