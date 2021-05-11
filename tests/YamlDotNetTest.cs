using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// Test the YamlDotNet YAML importer / exporter with a very basic file

// Unity package: https://assetstore.unity.com/packages/tools/integration/yamldotnet-for-unity-36292#releases
// GitHub original project: https://github.com/aaubry/YamlDotNet
// Wiki: https://github.com/aaubry/YamlDotNet/wiki
// Samples: https://github.com/aaubry/YamlDotNet/wiki/Samples
// Nice sample: https://dotnetfiddle.net/CQ7ZKi

// Version 6.1.3 of the Unity package is a port of version 6.1.2 of YamlDotNet
// As of 2021-05-11, the current version of YamlDotNet is 11.1, so the Unity port
// is quite behind...

namespace YamlDotNetTests
{
    [TestFixture]
    public class BasicYamlDotNetTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        // See the "Deserialization from a string to an object" sample at https://github.com/aaubry/YamlDotNet
        public void GitHubSample()
        {
            var yml = @"
receipt:    Oz-Ware Purchase Invoice
date:        2007-08-06
customer:
    given:   Dorothy
    family:  Gale

items:
    - part_no:   A4786
      descrip:   Water Bucket (Filled)
      price:     1.47
      quantity:  4

    - part_no:   E1628
      descrip:   High Heeled ""Ruby"" Slippers
      price:     100.27
      quantity:  1

bill-to:  &id001
    street: |
            123 Tornado Alley
            Suite 16
    city:   East Westville
    state:  KS

ship-to:  *id001

specialDelivery:  >
    Follow the Yellow Brick
    Road to the Emerald City.
    Pay no attention to the
    man behind the curtain.
";

            // Setup the input
            var input = new StringReader(yml);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // Examine the stream
            var mapping = (YamlMappingNode) yaml.Documents[0].RootNode;

            using var topList = mapping.GetEnumerator();
            topList.Reset();

            topList.MoveNext();
            var item1 = topList.Current;
            Assert.AreEqual(YamlNodeType.Scalar, item1.Key.NodeType);
            Assert.AreEqual("receipt", ((YamlScalarNode) item1.Key).Value);
            Assert.AreEqual(YamlNodeType.Scalar, item1.Value.NodeType);
            Assert.AreEqual("Oz-Ware Purchase Invoice", ((YamlScalarNode) item1.Value).Value);

            topList.MoveNext();
            var item2 = topList.Current;
            Assert.AreEqual(YamlNodeType.Scalar, item2.Key.NodeType);
            Assert.AreEqual("date", ((YamlScalarNode) item2.Key).Value);
            Assert.AreEqual(YamlNodeType.Scalar, item2.Value.NodeType);
            Assert.AreEqual("2007-08-06", ((YamlScalarNode) item2.Value).Value);

            topList.MoveNext();
            var item3 = topList.Current;
            Assert.AreEqual(YamlNodeType.Scalar, item3.Key.NodeType);
            Assert.AreEqual("customer", ((YamlScalarNode) item3.Key).Value);
            Assert.AreEqual(YamlNodeType.Mapping, item3.Value.NodeType);
            Assert.AreEqual(2, ((YamlMappingNode) item3.Value).Children.Count);

            // As a dictionary (more convenient: direct access and less type casting)
            var rootList = ((YamlMappingNode) yaml.Documents[0].RootNode).Children;
            var root1 = rootList["receipt"];
            Assert.AreEqual(YamlNodeType.Scalar, root1.NodeType);
            Assert.AreEqual("Oz-Ware Purchase Invoice", ((YamlScalarNode) root1).Value);

            var root4 = rootList["items"];
            Assert.AreEqual(YamlNodeType.Sequence, root4.NodeType);
            Assert.AreEqual(2, ((YamlSequenceNode) root4).Children.Count);

            var root5 = rootList["ship-to"]; // Aliases are resolved transparently
            Assert.AreEqual(YamlNodeType.Mapping, root5.NodeType);
            Assert.AreEqual(3, ((YamlMappingNode) root5).Children.Count);

            var root6 = rootList["ship-to"]; // Aliases are resolved transparently
            Assert.AreEqual(YamlNodeType.Mapping, root6.NodeType);
        }


        private class FileData
        {
            public string Receipt { get; set; }
            public string Date { get; set; }
            public Customer Customer { get; set; }
            public List<Item> Items { get; set; }
        }

        private class Customer
        {
            public string Given { get; set; }
            public string Family { get; set; }
        }

        private class Item : Dictionary<string, string>
        {
        }

        [Test]
        // See the "Deserialization from a string to an object" sample at https://github.com/aaubry/YamlDotNet
        public void DeserializerSample()
        {
            var yml = @"
receipt:    Oz-Ware Purchase Invoice
date:        2007-08-06
customer:
    given:   Dorothy
    family:  Gale

items:
    - part_no:   A4786
      descrip:   Water Bucket (Filled)
      price:     1.47
      quantity:  4
      quality:   new

    - part_no:   E1628
      descrip:   High Heeled ""Ruby"" Slippers
      price:     100.27
      quantity:  1
";
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new UnderscoredNamingConvention())
                .Build();
            var p = deserializer.Deserialize<FileData>(yml);
            var c = p.Customer;
            var i = p.Items;
            Assert.AreEqual("Dorothy", c.Given);
            Assert.AreEqual("new", i[0]["quality"]);
            Assert.AreEqual("E1628", i[1]["part_no"]);
        }
    }
}
