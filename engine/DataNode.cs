using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
        public string StringValue { get; set; }
        public float FloatValue { get; set; }
        public string ToString();
        public int Count { get; }
        public IDataNode this[int index] { get; set; }
        public IDataNode this[string key] { get; set; }

        /// <summary>
        /// Deep Clone
        /// </summary>
        /// <returns>Cloned data structure</returns>
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
        public ValueKind Kind { get; set; }

        public NodeType Type
        {
            get => NodeType.Value;
        }

        public DataValue(string value)
        {
            SetStringValue(value);
        }

        public DataValue(float value)
        {
            SetFloatValue(value);
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

        public string StringValue
        {
            get => _stringValue;
            set => SetStringValue(value);
        }

        protected void SetStringValue(string value)
        {
            Kind = ValueKind.String;
            _stringValue = value;
            float fValue = 0.0f;
            if (Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out fValue))
            {
                SetFloatValue(fValue);
            }
        }

        public float FloatValue
        {
            get => _floatValue;
            set => SetFloatValue(value);
        }

        protected void SetFloatValue(float value)
        {
            Kind = ValueKind.Float;
            _floatValue = value;
            _stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return StringValue;
        }

        public int Count
        {
            get => 0;
        }

        public IDataNode this[int index]
        {
            // We don't care about the index
            get => this;
            set => SetStringValue(value.StringValue);
        }

        public IDataNode this[string key]
        {
            // We don't care about the key
            get => this;
            set => SetStringValue(value.StringValue);
        }
    }

    public class DataList : List<IDataNode>, IDataNode
    {
        public NodeType Type
        {
            get => NodeType.List;
        }

        public string StringValue
        {
            get => "";
            set { }
        }

        public float FloatValue
        {
            get => 0.0f;
            set { }
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
                else if (item is double dd)
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

        public IDataNode this[string key]
        {
            // Try to interpret the key as an int
            get
            {
                int index;
                if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                {
                    return base[index];
                }

                return null!;
            }
            set
            {
                int index;
                if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                {
                    base[index] = value;
                }
            }
        }
    }

    public class DataDictionary : Dictionary<string, IDataNode>, IDataNode
    {
        public NodeType Type
        {
            get => NodeType.Dictionary;
        }

        public string StringValue
        {
            get => "";
            set { }
        }

        public float FloatValue
        {
            get => 0.0f;
            set { }
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

        public IDataNode this[int index]
        {
            get => this.ElementAt(index).Value;
            set
            {
                var kp = this.ElementAt(index);
                base[kp.Key] = value;
            }
        }
    }
}
