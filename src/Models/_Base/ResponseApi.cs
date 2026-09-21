using System.Text.Json.Serialization;

namespace api_finances.src.Models.Base
{
    public class ResponseApi<TData>
    {
        private readonly int statusCode = 200;

        [JsonConstructor]
        public ResponseApi() => statusCode = 200;

        public ResponseApi(TData? data, int code = 200, string? message = null, string? logEvent = null, dynamic? initialData = null)
        {
            Data = data;
            statusCode = code;
            StatusCode = code;
            Message = message;
            LogEvent = logEvent ?? "";
            InitialData = initialData;
        }

        public TData? Data { get; set; }
        
        public string? Message { get; set; }

        [JsonIgnore]
        public string LogEvent { get; set; } = string.Empty;

        [JsonIgnore]
        public dynamic? InitialData { get; set; }

        [JsonIgnore]
        public bool IsSuccess => statusCode is >= 200 and <= 299;

        [JsonIgnore]
        public int StatusCode;

        [JsonIgnore]
        public virtual dynamic Result => new{
            Message,
            Data
        };
    }
}