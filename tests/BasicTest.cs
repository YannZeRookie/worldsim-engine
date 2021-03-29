using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ChoETL;
using Newtonsoft.Json.Linq;

// Test the YAML importer / exporter with a very basic file

namespace ChoETLBasicTests
{
    public class Location
    {
        public Location()
        {
        }

        public Location(string city, string state, string country)
        {
            this.City = city;
            this.State = state;
            this.Country = country;
        }

        [ChoYamlRecordField] public string City { get; set; }
        [ChoYamlRecordField] public string State { get; set; }
        [ChoYamlRecordField] public string Country { get; set; }
    }

    public class Birth
    {
        private Location _birthLocation;
        private DateTime _date;

        public Birth()
        {
            this._birthLocation = new Location();
        }

        [ChoYamlRecordField]
        public DateTime Date
        {
            get => _date;
            set => _date = value;
        }

        [ChoYamlRecordField]
        public string City
        {
            get => this._birthLocation.City;
            set => this._birthLocation.City = value;
        }

        [ChoYamlRecordField]
        public string State
        {
            get => this._birthLocation.State;
            set => this._birthLocation.State = value;
        }

        [ChoYamlRecordField]
        public string Country
        {
            get => this._birthLocation.Country;
            set => this._birthLocation.Country = value;
        }
    }

    public class Person
    {
        [ChoYamlRecordField] public int Id { get; set; }
        [ChoYamlRecordField] public string Name { get; set; }
        [ChoYamlRecordField] public Birth Birth { get; set; }
        [ChoYamlRecordField] public int[] Ages { get; set; }
        [ChoYamlRecordField] public Location[] Homes { get; set; }
    }

    public class UserScore
    {
        public int id { get; set; }
        public int value { get; set; }
    }

    public class UserInfo
    {
        public string name { get; set; }
        public string teamname { get; set; }
        public string email { get; set; }
        public int[] players { get; set; }
        public UserScore[] scores { get; set; }
    }

    public class PeopleInfo
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class CityInfo
    {
        public string id { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
    }

    public class TwoList
    {
        public DateTime creation { get; set; }
        public string author { get; set; }
        public PeopleInfo[] people { get; set; }
        public CityInfo[] cities { get; set; }
    }

    [TestFixture]
    public class BasicYamlTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ReadPeople()
        {
            var parser = new ChoYamlReader<Person>("../../../fixtures/basic.yaml");
            List<Person> els = new List<Person>();
            foreach (var e in parser.WithYamlPath("$.people[*]"))
            {
                els.Add(e);
            }

            Person p = els[0];
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual(1, els[0].Id);
            Assert.AreEqual("Tom", els[0].Name);
            Assert.AreEqual(new DateTime(1965, 05, 22), els[0].Birth.Date);
            Assert.AreEqual(2, els[1].Id);
            Assert.AreEqual("Mark", els[1].Name);
        }

        [Test]
        public void TestSubNodes()
        {
            {
                string yaml = @"
users:
    - name: 1
      teamname: Tom
      email: tom@gmail.com
      players: [1, 2]
      scores:
        - id: 1
          value: 100
        - id: 2
          value: 200
    - name: 2
      teamname: Mark
      email: mark@gmail.com
      players: [3, 4, 5]
      scores:
        - id: 3
          value: 300
";
                using (var r = ChoYamlReader<UserInfo>.LoadText(yaml)
                    .WithYamlPath("$.users[*]")
                )
                {
                    foreach (var rec in r)
                        Console.WriteLine(rec.Dump());
                }
            }
        }

        [Test]
        public void TestTwoLists()
        {
            string yaml = @"
creation: 2020-12-26
author: YannZeRookie
people:
  - id: 1
    name: Tom
  - id: 2
    name: Mark
cities:
  - id: 1
    city: San Francisco
    state: CA
    country: USA
  - id: 2
    city: Palo Alto
    state: CA
    country: USA
  - id: 3
    city: Minneapolis
    state: MN
    country: USA
";
            using (var r = ChoYamlReader<TwoList>.LoadText(yaml))
            {
                var rec = r.First();
                Assert.AreEqual("YannZeRookie", rec.author);
                Assert.AreEqual(2, rec.people.Length);
                Assert.AreEqual(3, rec.cities.Length);
            }
        }


        [Test]
        public void ReadOneField()
        {
            var parser = new ChoYamlReader("../../../fixtures/basic.yaml");
            foreach (var e in parser.WithField("country", fieldType: typeof(string)))
            {
                Assert.AreEqual("country", e.Keys[0]);
                Assert.AreEqual("USA", e.Values[0]);
            }
        }

        [Test]
        public void ReadManyFields()
        {
            using (var parser = new ChoYamlReader("../../../fixtures/basic.yaml")
                .WithYamlPath("$.^"))
            {
                foreach (var e in parser)
                    Console.WriteLine(e.Dump());
            }
        }

        [Test]
        public void ReadFullGeneric()
        {
            using (var parser = new ChoYamlReader("../../../fixtures/basic.yaml"))
            {
                foreach (var e in parser)
                {
                    Console.WriteLine(e.Dump());
                }
            }
        }

        class PersonData
        {
            public DateTime Creation { get; set; }
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void ReadEntireFile1()
        {
            string yaml = @"
creation: 2020-12-26
firstname: John
lastname: Doe
age: 35";
            using (var parser = ChoYamlReader<PersonData>.LoadText(yaml))
            {
                foreach (var e in parser)
                {
                    Console.WriteLine(e.Firstname); // John
                    Console.WriteLine(e.Lastname); // Doe
                    Assert.AreEqual("John", e.Firstname);
                    Assert.AreEqual("Doe", e.Lastname);
                }
            }
        }

        [Test]
        public void ReadEntireFileAnonymous()
        {
            string yaml = @"
creation: 2020-12-26
firstname: John
lastname: Doe
age: 35";
            using (var parser = ChoYamlReader<IDictionary<string, object>>.LoadText(yaml))
            {
                IDictionary<string, object> e = parser.First(); // This shows how just to get what we are interested in
                string firstname = (string) e["firstname"]; // John
                DateTime creation = DateTime.Parse((string) e["creation"]); // 2020-12-26
                int age = (int) e["age"]; // 35
                Console.WriteLine(e.Dump());
                Assert.AreEqual("John", firstname);
                Assert.AreEqual(new DateTime(2020, 12, 26), creation);
                Assert.AreEqual(35, age);
            }
        }

        class ChildData
        {
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public DateTime Dob { get; set; }
        }

        class AddressData
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Country { get; set; }
        }

        class ParentData
        {
            public DateTime Creation { get; set; }
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public int Age { get; set; }
            public ChildData[] Children { get; set; }
            public AddressData[] Addresses { get; set; }
        }

        [Test]
        public void ReadEntireFile2()
        {
            string yaml = @"
creation: 2020-12-26
firstname: John
lastname: Doe
age: 35
children:
- firstname: Emmanuel
  lastname: Doe
  dob: 1990-01-02
- firstname: Elise
  lastname: Doe
  dob: 1996-01-02
addresses:
- street: 1234 California Ave
  city: San Francisco
  state: CA
  zip: 98765
  country: USA
- street: 666 Midtown Ct
  city: Palo Alto
  zip: 94444
  state: CA
  country: USA
";
            using (var parser = ChoYamlReader<ParentData>.LoadText(yaml))
            {
                foreach (var e in parser)
                {
                    Console.WriteLine(e.Firstname); // John
                    Console.WriteLine(e.Lastname); // Doe
                    Console.WriteLine(e.Children[0].Firstname); // Emmanuel
                    Console.WriteLine(e.Addresses[0].Street); // 1234 California Ave
                    Assert.AreEqual("John", e.Firstname);
                    Assert.AreEqual("Doe", e.Lastname);
                    Assert.AreEqual("Emmanuel", e.Children[0].Firstname);
                    Assert.AreEqual("1234 California Ave", e.Addresses[0].Street);
                    Assert.AreEqual(2, e.Children.Length);
                    Assert.AreEqual(2, e.Addresses.Length);
                }
            }
        }


        class ChildrenData
        {
            public IList<object> Children { get; set; }
        }

        [Test]
        public void ReadAnonymousLists()
        {
            string yaml = @"
children:
- firstname: Emmanuel
  lastname: Doe
  dob: 1990-01-02
- firstname: Elise
  lastname: Doe
  dob: 1996-10-02
";
            using (var parser = ChoYamlReader<ChildrenData>.LoadText(yaml))
            {
                foreach (var e in parser)
                {
                    IDictionary<object, object> firstChild = (IDictionary<object, object>) e.Children[0];
                    string firstname = (string) firstChild["firstname"]; // Emmanuel
                    Console.WriteLine(e.Dump());
                    Assert.AreEqual("Emmanuel", firstname);
                }
            }
        }

        class OwnerData
        {
            public PossessionData[] Possessions { get; set; }
        }

        class PossessionData
        {
            public string Type { get; set; }
            public IDictionary<string, object> Description { get; set; }
        }

        [Test]
        public void ReadDynamicData()
        {
            string yaml = @"
possessions:
- type: car
  description:
    color: blue
    doors: 4
- type: computer
  description:
    disk: 1 TB
    memory: 16 MB
";
            using (var parser = ChoYamlReader<OwnerData>.LoadText(yaml))
            {
                foreach (var e in parser)
                {
                    string carColor = (string) e.Possessions[0].Description["color"]; // blue
                    foreach (var p in e.Possessions)
                    {
                        Console.WriteLine(p.Description.Dump());
                    }

                    Assert.AreEqual("blue", carColor);
                }
            }
        }

        /// <summary>
        /// Test the type of classes that the importer builds when doing a generic import 
        /// </summary>
        [Test]
        public void TestClassesDynamicDataFromString()
        {
            string yaml = @"
hello: world
list:
    - type: car
      color: blue
    - type: computer
      color: gray
";
            using (var parser = ChoYamlReader.LoadText(yaml))
            {
                ParseDynamicClasses(parser.First());
            }
        }

        [Test]
        public void TestClassesDynamicDataFromFile()
        {
            using (var parser = new ChoYamlReader("../../../fixtures/basic2.yaml"))
            {
                ParseDynamicClasses(parser.First());
            }
        }

        private void ParseDynamicClasses(dynamic elements)
        {
            Assert.IsInstanceOf<IDictionary<string, object>>(elements);
            var elementsDict =
                (IDictionary<string, object>) elements; // You have to do this cast if you want to be able to use the dictionary with some methods such as First()
            Assert.IsInstanceOf<string>(elementsDict.First().Key); // Here the key is a string, which is good
            Assert.IsInstanceOf<IList<object>>(elements["list"]); // But this still works
            var list = (IList<object>) elements[
                "list"]; // You have to do this cast if you want to be able to use the list
            Assert.IsInstanceOf<IDictionary<object, object>>(list.First());
            var item = (IDictionary<object, object>) list
                .First(); // You have to do this cast if you want to be able to use the dictionary
            var el = item.First();
            Assert.AreEqual("type",
                (string) el.Key); // At this level, they key is an object instead of a string - which is too bad because you can easily refer to elements
            Assert.AreEqual("car", (string) el.Value);
        }

        /// <summary>
        /// This is the best way to deal with a dynamic list of dictionaries
        /// This is better than the method above
        /// </summary>
        public class DynamicData
        {
            public string Hello { get; set; }
            public IDictionary<string, object>[] List { get; set; }
        }

        [Test]
        public void TestClassesDynamicDataUsingRecords()
        {
            string yaml = @"
hello: world
list:
    - type: car
      color: blue
    - type: computer
      color: gray
";
            using (var parser = ChoYamlReader<DynamicData>.LoadText(yaml))
            {
                var elements = parser.First();
                Assert.IsInstanceOf<DynamicData>(elements);
                Assert.AreEqual("world", elements.Hello);
                var item = elements.List.First();
                var el = item.First();
                Assert.AreEqual("type", el.Key);
                Assert.AreEqual("car", (string) el.Value);
            }
        }
        
        public class DynamicNestedData
        {
            public string Jm2_Id { get; set; }
            public IDictionary<string, object> Jm2_Init { get; set; }
        }


        [Test]
        public void TestNestedDynamicClasses()
        {
            string yaml = @"
jm2_id: factory
jm2_init:
    opex:
      - resource_id: coal
        consumption: 50
      - resource_id: o2
        consumption: 100
    output:
      - resource_id: co2
        production: 150
";
            using (var parser = ChoYamlReader<DynamicNestedData>.LoadText(yaml))
            {
                var elements = parser.First();
                Assert.IsInstanceOf<DynamicNestedData>(elements);
                Assert.AreEqual("factory", elements.Jm2_Id);
                var item = elements.Jm2_Init.First();
                Assert.AreEqual("opex", item.Key);
                Assert.IsInstanceOf<JArray>(item.Value);    // I wished it was a List or a system array
            }
        }
    }
}
