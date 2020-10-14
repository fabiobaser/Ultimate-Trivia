using Newtonsoft.Json;

namespace UltimateTrivia.Services
{
    public class JsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _options;

        public JsonSerializer(JsonSerializerSettings options)
        {
            _options = options;
        }
        
        public string Serialize<T>(T value, bool indent = false)
        {
            if (indent)
            {
                _options.Formatting = Formatting.Indented;
            }
            
            return JsonConvert.SerializeObject(value, _options);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _options);
        }
    }
}