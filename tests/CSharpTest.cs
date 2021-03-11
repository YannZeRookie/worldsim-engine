using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CSharpTest
{
    /// <summary>
    /// Code to test my understanding of C# 
    /// </summary>
    [TestFixture]
    public class CSharpTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestInterfaceInstancianting()
        {
            TestList list = new TestList();
            TestItem item1 = new TestItem("Anne");
            TestItem item2 = new TestItem("Bob");
            TestItem item3 = new TestItem("Charles");
            IList<ITestItem> testList = list.GetList();
            testList.Add(item1);
            list.AddItem(item2);
            list.AddTestItem(item3);
            Assert.AreEqual("TestItem: Anne", list.GetItem(0).Talk());
            Assert.AreEqual("TestItem: Bob", list.GetItem(1).Talk());
            Assert.AreEqual("TestItem: Charles", list.GetItem(2).Talk());
        }

        [Test]
        public void TestInterfaceInstancianting2()
        {
            TestList2 list = new TestList2();
            list.AddItem("Anne");
            Assert.AreEqual("TestItem: Anne", list.GetItem(0).Talk());
            IList<ITestItem> ilist = list.GetList();
            ilist[0].SetName("Alpha");
            Assert.AreEqual("TestItem: Alpha", list.GetItem(0).Talk());
        }
    }

    public interface ITestItem
    {
        public void SetName(string name);
        public string Talk();
    }

    public class TestItem : ITestItem
    {
        private string _name;

        public TestItem(string name)
        {
            _name = name;
        }

        public void SetName(string name)
        {
            _name = name;
        }

        public string Talk()
        {
            return "TestItem: " + _name;
        }
    }

    public interface ITestList
    {
        IList<ITestItem> GetList();
    }

    public class TestList : ITestList
    {
        private List<ITestItem> _list;

        public TestList()
        {
            _list = new List<ITestItem>();
        }

        public IList<ITestItem> GetList()
        {
            return _list;
        }

        public ITestItem GetItem(int index)
        {
            return _list[index];
        }

        public void AddItem(ITestItem item)
        {
            _list.Add(item);
        }

        public void AddTestItem(TestItem item)
        {
            _list.Add(item);
        }
    }

    public class TestList2 : ITestList
    {
        private List<TestItem> _list;

        public TestList2()
        {
            _list = new List<TestItem>();
        }

        public ITestItem GetItem(int index)
        {
            return _list[index];
        }

        public void AddItem(string name)
        {
            TestItem item = new TestItem(name);
            _list.Add(item);
        }

        public void Try()
        {
            IList<TestItem> l1 = _list;
            IList<ITestItem> l2 = _list.ToList<ITestItem>();
            List<TestItem> l3 = _list;
            List<ITestItem> l4 = _list.ToList<ITestItem>();
            TestItem[] a = new TestItem[2];
            ITestItem[] a2 = a;
        }
        
        public IList<ITestItem> GetList()
        {
            return _list.ToList<ITestItem>();
            /* This also works but is cumbersome:
            IList<ITestItem> result = new List<ITestItem>();
            foreach (var item in _list)
            {
                result.Add(item);
            }
            return result;
            */
        }
    }
    
    // Covariant return type in C#9 won't work with interfaces it seems
    
    public abstract class ATestItem
    {
        public abstract string Talk();
    }

    public class NewTestItem : ATestItem
    {
        public override string Talk()
        {
            return "NewTestItem";
        }

        public static IList<ATestItem> TryIt()
        {
            List<NewTestItem> list = new List<NewTestItem>();
            IList<ATestItem> l = list.ToList<ATestItem>();
            return l;
        }
    }


}