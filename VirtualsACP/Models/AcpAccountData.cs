using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public class AcpAccountData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("clientAddress")]
    public string ClientAddress { get; set; } = string.Empty;

    [JsonPropertyName("providerAddress")]
    public string ProviderAddress { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

