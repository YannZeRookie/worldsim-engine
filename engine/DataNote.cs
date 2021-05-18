using System;
using System.Collections.Generic;
using System.Globalization;

namespace WorldSim.API
{
    public enum NodeType
    {
        Value,
        List,
        Dictionary
    }

    /// <summary>
    /// This are helper classes that are intended to handle generic hierarchies
    /// of data, with nodes being values, lists or dictionaries - such as
    /// what YAML or JSON can do.
    /// They are designed to be easy to traverse and easy to get values from (either
    /// as strings or floats).
    /// The goal is to be independent of any YAML library implementation.
    /// </summary>
    public interface IDataNode
    {
        public NodeType Type { get; }
        public string StringValue();
        public float FloatValue();
        public string ToString();
    }

    public enum ValueKind
    {
        String,
        Float
    }

    public class DataValue : IDataNode
    {
        private string _stringValue;
        private float _floatValue;

        public NodeType Type
        {
            get => NodeType.Value;
        }

        public DataValue(string value)
        {
            Kind = ValueKind.String;
            _stringValue = value;

            // We're going to **try** to evaluate it as a float
            float fValue = 0.0f;
            if (Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out fValue))
            {
                Kind = ValueKind.Float;
                _floatValue = fValue;
            }
        }

        public DataValue(float value)
        {
            Kind = ValueKind.Float;
            _floatValue = value;
            _stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public ValueKind Kind { get; }

        public string StringValue()
        {
            return _stringValue;
        }

        public float FloatValue()
        {
            return _floatValue;
        }

        public override string ToString()
        {
            return StringValue();
        }
    }

    public class DataList : List<IDataNode>, IDataNode
    {
        public NodeType Type
        {
            get => NodeType.List;
        }

        public string StringValue()
        {
            return "";
        }

        public float FloatValue()
        {
            return 0.0f;
        }

        public override string ToString()
        {
            return "DataList: " + Count;
        }

        public static DataList ConvertGenericData(List<object> generic)
        {
            DataList result = new DataList();
            foreach (var item in generic)
            {
                if (item is string s)
                {
                    result.Add(new DataValue(s));
                }
                else if (item is Dictionary<object, object> d)
                {
                    DataDictionary dict = DataDictionary.ConvertGenericData(d);
                    result.Add(dict);
                }
                else if (item is List<object> l)
                {
                    DataList list = DataList.ConvertGenericData(l);
                    result.Add(list);
                }
            }

            return result;
        }
    }

    public class DataDictionary : Dictionary<string, IDataNode>, IDataNode
    {
        public NodeType Type
        {
            get => NodeType.Dictionary;
        }

        public string StringValue()
        {
            return "";
        }

        public float FloatValue()
        {
            return 0.0f;
        }

        public override string ToString()
        {
            return "DataDictionary: " + Count;
        }

        public void Add(string key, string value)
        {
            Add(key, new DataValue(value));
        }

        public void Add(string key, float value)
        {
            Add(key, new DataValue(value));
        }

        public static DataDictionary ConvertGenericData(Dictionary<object, object> generic)
        {
            DataDictionary result = new DataDictionary();
            foreach (var kp in generic)
            {
                if (kp.Key is string k1 && kp.Value is string s)
                {
                    result.Add(k1, new DataValue(s));
                }
                else if (kp.Key is string k2 && kp.Value is Dictionary<object, object> d)
                {
                    DataDictionary dict = DataDictionary.ConvertGenericData(d);
                    result.Add(k2, dict);
                }
                else if (kp.Key is string k3 && kp.Value is List<object> l)
                {
                    DataList list = DataList.ConvertGenericData(l);
                    result.Add(k3, list);
                }
            }

            return result;
        }
    }
}
