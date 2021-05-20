using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
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

        [Test]
        // Are lists referenced or duplicated? The answer: they are referenced
        public void TestListReferencing()
        {
            List<string> list = new List<string>() {"hello"};
            ListHolder holder = new ListHolder(list);
            Assert.AreEqual(1, holder.List.Count);
            list.Add("World");
            Assert.AreEqual(2, holder.List.Count);
        }

        [Test]
        public void TestNullableFloats()
        {
            float? a = null;
            float? b = null;
            float? c = a / b;
            Assert.IsNull(c);
            float? d = 1.0f / b;
            Assert.IsNull(d);
            float? r = 0.0f;
            float? e = a / r;
            Assert.IsNull(e);
        }

        [Test]
        public void TestEnumerableArray()
        {
            // This works with a 1 dimension array
            TestItem[] a1 = new TestItem[2];
            Assert.IsInstanceOf<IEnumerable<TestItem>>(a1);
            // But it won't work with a two-dimensional array!
            TestItem[,] a2 = new TestItem[2, 2];
            // This works:
            Assert.IsInstanceOf<IEnumerable>(a2);
            // This does not:
            //Assert.IsInstanceOf<IEnumerable<TestItem>>(a2);
            // See See https://stackoverflow.com/questions/275073/
        }

        /// <summary>
        /// See https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing
        /// </summary>
        [Test]
        public void TestBoxingUnboxing()
        {
            float f = 3.14f;
            object fo = f;  // Boxing
            Assert.IsInstanceOf<float>(f);      // Expected
            Assert.IsInstanceOf<object>(fo);    // Expected
            Assert.IsInstanceOf<float>(fo);     // Implicit unboxing happens here
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

    // Are lists referenced or duplicated?
    public class ListHolder
    {
        public IList<string> List;

        public ListHolder(IList<string> l)
        {
            this.List = l;
        }
    }

    //---------------------------------------------------------------------------
    // Understanding interfaces vs static methods vs factories vs implementations
    //---------------------------------------------------------------------------

    public interface IPerson
    {
        string Name();

        public static IPerson CreateCitizen()
        {
            return new Citizen();
        }
    }

    class Citizen : IPerson
    {
        public string Name()
        {
            return "John Doe";
        }
    }

    class Nation : IList<Citizen>
    {
        private readonly IList<Citizen> _list = new List<Citizen>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _list).GetEnumerator();
        }

        public IEnumerator<Citizen> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void Add(Citizen item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(Citizen item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(Citizen[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(Citizen item)
        {
            return _list.Remove(item);
        }

        public int Count => _list.Count;

        public bool IsReadOnly => _list.IsReadOnly;

        public int IndexOf(Citizen item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, Citizen item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public Citizen this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
    }
}
