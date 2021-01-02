using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ChoETL;

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
        public void ReadGeneric()
        {
            using (var parser = new ChoYamlReader("../../../fixtures/basic.yaml"))
            {
                foreach (var e in parser)
                {
                    Console.WriteLine(e.Dump());
                }
            }
        }
    }
}