using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public class IACPAgent
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("walletAddress")]
    public string WalletAddress { get; set; } = string.Empty;

    [JsonPropertyName("offerings")]
    public List<ACPJobOffering> Offerings { get; set; } = new();

    [JsonPropertyName("twitterHandle")]
    public string? TwitterHandle { get; set; }

    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }

    [JsonPropertyName("isVirtualAgent")]
    public bool? IsVirtualAgent { get; set; }

    [JsonPropertyName("profilePic")]
    public string? ProfilePic { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("tokenAddress")]
    public string? TokenAddress { get; set; }

    [JsonPropertyName("ownerAddress")]
    public string? OwnerAddress { get; set; }

    [JsonPropertyName("cluster")]
    public string? Cluster { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("virtualAgentId")]
    public string? VirtualAgentId { get; set; }

    [JsonPropertyName("metrics")]
    public Dictionary<string, object>? Metrics { get; set; }

    [JsonPropertyName("contractAddress")]
    public string? ContractAddress { get; set; }

    [JsonPropertyName("processingTime")]
    public string? ProcessingTime { get; set; }
}
