using System.Text.Json.Serialization;

namespace VirtualsACP.Tests;

public class TestConfig
{
    // Buyer (client) credentials
    [JsonPropertyName("buyerPrivateKey")]
    public string BuyerPrivateKey { get; set; } = string.Empty;

    [JsonPropertyName("buyerWalletAddress")]
    public string BuyerWalletAddress { get; set; } = string.Empty;

    // Seller (provider) credentials
    [JsonPropertyName("sellerPrivateKey")]
    public string SellerPrivateKey { get; set; } = string.Empty;

    [JsonPropertyName("sellerWalletAddress")]
    public string SellerWalletAddress { get; set; } = string.Empty;

    // Legacy support (for Phase 1 tests)
    [JsonPropertyName("walletPrivateKey")]
    public string? WalletPrivateKey { get; set; }

    [JsonPropertyName("agentWalletAddress")]
    public string? AgentWalletAddress { get; set; }

    [JsonPropertyName("providerAddress")]
    public string? ProviderAddress { get; set; }

    [JsonPropertyName("evaluatorAddress")]
    public string? EvaluatorAddress { get; set; }

    [JsonPropertyName("network")]
    public string Network { get; set; } = "base";
}

