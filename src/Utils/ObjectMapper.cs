using System.Text.Json;

namespace api_finances.src.Utils
{
    public static class ObjectMapper
    {
        public static TDestination Map<TSource, TDestination>(TSource source)
            where TDestination : new()
        {
            if (source == null) return new TDestination();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (source is JsonElement jsonElement)
            {
                return jsonElement.Deserialize<TDestination>(options) ?? new TDestination();
            }

            var json = JsonSerializer.Serialize(source, options);
            return JsonSerializer.Deserialize<TDestination>(json, options) ?? new TDestination();
        }
    }
}

