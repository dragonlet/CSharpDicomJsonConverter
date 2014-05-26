using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonConverter
{
    public class DicomJObject
    {
        private readonly Dictionary<String, DicomJElement> _dicomJElements = new Dictionary<string, DicomJElement>();

        public Dictionary<String, DicomJElement> DicomJElements { get { return _dicomJElements;  } }

        public DicomJObject(String json)
        {
            this.Parse(json);
        }

        public DicomJObject()
        {
        }

        public String Tag(DicomTag tag)
        {
            return String.Format("{0:X04}{1:X04}", tag.Group, tag.Element);
        }

        public void Load(String fileName)
        {
            using (StreamReader f = File.OpenText(fileName))
            {
                string j = f.ReadToEnd();
                f.Close();
                Parse(j);
            }
        }

        public Dictionary<String, DicomJElement> Parse(String json)
        {
            var elements = JsonConvert.DeserializeObject<Dictionary<String, JObject>>(json);

            foreach (var item in elements)
            {
                string vr = item.Value["vr"].ToString();
                var el = new DicomJElement
                {
                    Element = item.Value
                };
                
                if (vr == "SQ")
                {
                    JToken vals = item.Value["Values"];
                    foreach (JToken v in vals)
                    {
                        var sqo = new DicomJObject();
                        string vitem = v.ToString();
                        Dictionary<string, DicomJElement> e2 = sqo.Parse(vitem);
                        el.sqElements.Add(e2);
                    }
                }

                el.Tag = item.Key;
                _dicomJElements.Add(item.Key, el);
            }

            return _dicomJElements;
        }

        public JObject GetElement(String tag)
        {
            if (_dicomJElements == null) return null;
            if (_dicomJElements.ContainsKey(tag) == false) return null;
            return _dicomJElements[tag].Element;
        }

        public DicomJElement GetSQElement(String tag, int itemNo, String itemTag)
        {
            if (_dicomJElements == null) return null;
            if (_dicomJElements.ContainsKey(tag) == false) return null;
            DicomJElement sq = _dicomJElements[tag];
            Dictionary<string, DicomJElement> item = sq.sqElements[itemNo];
            DicomJElement el = item[itemTag];
            return el;
        }

        public String GetValue(String tag, int index = 0)
        {
            JObject elm = GetElement(tag);
            if (elm == null) return null;
            JToken vals = elm["Values"];
            int count = vals.Count();
            if (count == 0 || index >= count) return null;
            string v = vals[index].ToString();
            return v;
        }

        public String GetVR(String tag)
        {
            JObject elm = GetElement(tag);
            if (elm == null) return null;
            string val = elm["vr"].ToString();
            return val;
        }

        public Boolean IsSQ(String tag)
        {
            return (GetVR(tag) == "SQ");
        }
    }
}