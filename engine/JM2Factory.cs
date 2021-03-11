using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WorldSim.Engine
{
    public class JM2Factory : JM2
    {
        private Dictionary<string, float> _opex;
        private Dictionary<string, float> _output;
        private IDictionary<string, object> _init;

        public JM2Factory(IDictionary<string, object> init) : base()
        {
            Id = "factory";
            _init = init;
            _opex = new Dictionary<string, float>();
            _output = new Dictionary<string, float>();
            Restart();
        }

        public override void Restart()
        {
            _opex.Clear();
            foreach (JObject o in (IEnumerable) _init["opex"])
            {
                string resourceId = (string) ((JValue) o["resource_id"]).Value;
                _opex[resourceId] = Convert.ToSingle(((JValue) o["consumption"]).Value);
            }

            _output.Clear();
            foreach (JObject o in (IEnumerable) _init["output"])
            {
                string resource_id = (string) ((JValue) o["resource_id"]).Value;
                _output[resource_id] = Convert.ToSingle(((JValue) o["production"]).Value);
            }

            base.Restart();
        }

        public override void Step(Map map, IDictionary<string, float> stocks, Time currentTime, float annualDivider,
            IDictionary<string, float> output)
        {
            float efficiency = 1.0f;
            foreach (var supply in _opex)
            {
                float needs = supply.Value / annualDivider;
                float consumed = ConsumeResource(supply.Key, needs, map, stocks);
                if (needs > 0.0f)
                    efficiency = Math.Min(efficiency, consumed / needs); // We are only as strong as our weakest point
            }

            Efficiency = efficiency;

            foreach (var production in _output)
            {
                output[production.Key] = production.Value * efficiency / annualDivider;
            }
        }
    }
}