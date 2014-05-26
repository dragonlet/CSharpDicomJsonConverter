using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonConverter
{
    class WADOable
    {
        public void Add(DicomJElement element)
        {
            if (_dicomJElements.ContainsKey(element.Tag)) return;
            if (element.Element != null) _dicomJElements.Add(element.Tag, element.Element);
        }

        public void Add(String tag, JObject element)
        {
            if (_dicomJElements.ContainsKey(tag)) return;
            if (element != null) _dicomJElements.Add(tag, element);
        }

        public void Add(Dictionary<String, DicomJElement> elements, List<String> tagFilter)
        {
            foreach (var tag in tagFilter)
            {
                Add(elements[tag]);
            }
        }

        public String Serialize(Boolean indentedFormat = true)
        {
            return indentedFormat ? 
                JsonConvert.SerializeObject(_dicomJElements, Formatting.Indented) :
                JsonConvert.SerializeObject(_dicomJElements);
        }

        private readonly Dictionary<String, JObject> _dicomJElements = new Dictionary<string, JObject>();
    }
}
