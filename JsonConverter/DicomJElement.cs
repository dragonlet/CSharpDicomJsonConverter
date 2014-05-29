using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Dicom;

namespace JsonConverter
{

    public class DicomJElement
    {
        public String Tag = "";
        public JObject Element;
        public List<Dictionary<string, DicomJElement>> sqElements = new List<Dictionary<string, DicomJElement>>();

        public DicomJElement()
        {
        }

        public String GetString(int index = 0)
        {
            JObject elm = Element;
            if (elm == null) return null;
            JToken vals = elm["Values"];
            int count = vals.Count();
            if (count == 0 || index >= count) return null;
            string v = vals[index].ToString();
            return v;
        }

        public Double GetDouble(int index = 0)
        {
            JObject elm = Element;
            if (elm == null) return Double.NaN;
            JToken vals = elm["Values"];
            int count = vals.Count();
            if (count == 0 || index >= count) return Double.NaN;
            string v = vals[index].ToString();
            Double d;
            if (Double.TryParse(v, out d)) return d;
            return Double.NaN;
        }

        public int Count()
        {
            JObject elm = Element;
            if (elm == null) return 0;
            JToken vals = elm["Values"];
            return vals.Count();
        }

        public override string ToString()
        {
            return Element.ToString();
        }
    }
}