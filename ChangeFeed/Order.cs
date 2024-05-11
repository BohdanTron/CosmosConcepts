using Newtonsoft.Json;

namespace ChangeFeed
{
    public class Order
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; } = default!;

        [JsonProperty("user")]
        public string User { get; set; } = default!;

        [JsonProperty("created")]
        public DateTime Created { get; set; }
    }
}
