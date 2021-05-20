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

        /// <summary>
        /// Deep Clone
        /// </summary>
        /// <returns></returns>
        public IDataNode Clone();
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
        public ValueKind Kind { get; }

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

        public DataValue(DataValue value)
        {
            Kind = value.Kind;
            _stringValue = value._stringValue;
            _floatValue = value._floatValue;
        }

        public IDataNode Clone()
        {
            return new DataValue(this);
        }

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

        public IDataNode Clone()
        {
            DataList clone = new DataList();
            foreach (var item in this)
            {
                clone.Add(item.Clone());
            }
            return clone;
        }

        public void Add(string value)
        {
            Add(new DataValue(value));
        }

        public void Add(float value)
        {
            Add(new DataValue(value));
        }

        public void Add(IList<float> list)
        {
            foreach (float value in list)
            {
                Add(new DataValue(value));
            }
        }
        
        public void Add(IList<string> list)
        {
            foreach (string value in list)
            {
                Add(new DataValue(value));
            }
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
                else if (item is float f)
                {
                    result.Add(new DataValue(f));
                }
                else if ( item is double dd)
                {
                    result.Add(new DataValue(Convert.ToSingle(dd)));
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

        public IDataNode Clone()
        {
            DataDictionary clone = new DataDictionary();
            foreach (var item in this)
            {
                clone.Add(item.Key, item.Value.Clone());
            }
            return clone;
        }

        public void Add(string key, string value)
        {
            Add(key, new DataValue(value));
        }

        public void Add(string key, float value)
        {
            Add(key, new DataValue(value));
        }

        public void Add(string key, IDictionary<string, float> dict)
        {
            Add(key, CreateFrom(dict));
        }

        public void Add(string key, IDictionary<string, string> dict)
        {
            Add(key, CreateFrom(dict));
        }

        public void Add(IDictionary<string, float> dict)
        {
            foreach (KeyValuePair<string, float> item in dict)
            {
                Add(item.Key, new DataValue(item.Value));
            }
        }

        public void Add(IDictionary<string, string> dict)
        {
            foreach (KeyValuePair<string, string> item in dict)
            {
                Add(item.Key, new DataValue(item.Value));
            }
        }

        public static DataDictionary CreateFrom(IDictionary<string, float> dict)
        {
            DataDictionary result = new DataDictionary();
            result.Add(dict);
            return result;
        }

        public static DataDictionary CreateFrom(IDictionary<string, string> dict)
        {
            DataDictionary result = new DataDictionary();
            result.Add(dict);
            return result;
        }

        public static DataDictionary ConvertGenericData(Dictionary<object, object> generic)
        {
            DataDictionary result = new DataDictionary();
            foreach (var kp in generic)
            {
                if (kp.Key is string ks && kp.Value is string s)
                {
                    result.Add(ks, new DataValue(s));
                }
                else if (kp.Key is string kf && kp.Value is float f)
                {
                    result.Add(kf, new DataValue(f));
                }
                else if (kp.Key is string kd && kp.Value is double dd)
                {
                    result.Add(kd, new DataValue(Convert.ToSingle(dd)));
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
