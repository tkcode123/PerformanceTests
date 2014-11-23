using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.IO;

namespace TestJSON
{
    class Program
    {
        static void Main(string[] args)
        {
            var m = new MyValues() { D = "abc", S = Guid.NewGuid().ToString(), O = "/abc/def/ghi", P = "abcdefghijklmn", 
                                     T = new DateTimeOffset(DateTime.UtcNow) };

            var s = JsonConvert.SerializeObject(m);

            var c = new JsonConverter[] { new MyValuesConverter() };

            var start = DateTime.UtcNow;
            for (int i = 0; i < 1000000; i++)
            {
               // var y = JsonConvert.DeserializeObject<MyValues>(s, c);

                var y = MyValues.Read(s);
                if (string.Equals(m.S, y.S) == false || m.T.Equals(y.T) == false)
                    throw new InvalidOperationException();
            }
            var end = DateTime.UtcNow;
            Console.WriteLine("Took {0}", (end-start));
        }
    }

    
    [JsonConverter(typeof(MyValuesConverter))]
    class MyValues
    {
        [JsonProperty("D")]
        public string D { get; set; }
        [JsonProperty("S")]
        public string S { get; set; }
        [JsonProperty("O")]
        public string O { get; set; }
        [JsonProperty("P")]
        public string P { get; set; }
        [JsonProperty("T")]
        public DateTimeOffset T { get; set; }

        public static MyValues Read(string s)
        {
            var r = new JsonTextReader(new StringReader(s));
            MyValues v = null;
            while (r.Read())
            {
                JsonToken tok;
                switch (tok = r.TokenType)
                {
                    case JsonToken.StartObject:
                        v = new MyValues();
                        break;
                    case JsonToken.EndObject:
                        return v;
                    case JsonToken.PropertyName:
                        var key = r.Value.ToString();
                        if (key == "D")
                        {
                            v.D = r.ReadAsString();
                        }
                        else if (key == "S")
                        {
                            v.S = r.ReadAsString();
                        }
                        else if (key == "O")
                        {
                            v.O = r.ReadAsString();
                        }
                        else if (key == "P")
                        {
                            v.P = r.ReadAsString();
                        }
                        else if (key == "T")
                        {
                            var dto = r.ReadAsDateTimeOffset();
                            if (dto.HasValue)
                                v.T = dto.Value;
                        }
                        break;
                    case JsonToken.Comment:
                        break;
                    default:
                        break;
                }
            }
            return v;
        }
    }

    class MyValuesConverter : CustomCreationConverter<MyValues>
    {
        public override MyValues Create(Type objectType)
        {
            return new MyValues();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(MyValues) == objectType;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(MyValues))
            {
                // Load JObject from stream
                JObject jObject = JObject.Load(reader);

                // Create target object based on JObject
                var target = new MyValues();

                // Populate the object properties
                var t = jObject["D"];
                if (t != null)
                    target.D = t.ToString();
                t = jObject["S"];
                if (t != null)
                    target.S = t.ToString();
                t = jObject["P"];
                if (t != null)
                    target.P = t.ToString();
                t = jObject["O"];
                if (t != null)
                    target.O = t.ToString();
                t = jObject["T"];
                if (t != null)
                    target.T = ((DateTimeOffset?)t).Value;

                //serializer.Populate(jObject.CreateReader(), target);

                return target;
            }
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
