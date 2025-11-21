using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public class X402PayableRequest
{
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    [JsonPropertyName("maxTimeoutSeconds")]
    public int MaxTimeoutSeconds { get; set; }

    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;
}

public class X402Requirement
{
    [JsonPropertyName("scheme")]
    public string Scheme { get; set; } = string.Empty;

    [JsonPropertyName("network")]
    public string Network { get; set; } = string.Empty;

    [JsonPropertyName("maxAmountRequired")]
    public string MaxAmountRequired { get; set; } = string.Empty;

    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("payTo")]
    public string PayTo { get; set; } = string.Empty;

    [JsonPropertyName("maxTimeoutSeconds")]
    public int MaxTimeoutSeconds { get; set; }

    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("extra")]
    public Dictionary<string, string> Extra { get; set; } = new();

    [JsonPropertyName("outputSchema")]
    public object? OutputSchema { get; set; }
}

public class X402PayableRequirements
{
    [JsonPropertyName("x402Version")]
    public int X402Version { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("accepts")]
    public List<X402Requirement> Accepts { get; set; } = new();
}

public class X402PaymentMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("validAfter")]
    public string ValidAfter { get; set; } = string.Empty;

    [JsonPropertyName("validBefore")]
    public string ValidBefore { get; set; } = string.Empty;

    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;
}

public class X402Payment
{
    [JsonPropertyName("encodedPayment")]
    public string EncodedPayment { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public X402PaymentMessage Message { get; set; } = new();
}

public class X402PaymentResponse
{
    [JsonPropertyName("isPaymentRequired")]
    public bool IsPaymentRequired { get; set; }

    [JsonPropertyName("data")]
    public X402PayableRequirements Data { get; set; } = new();
}

public class IAcpJobX402PaymentDetails
{
    public bool IsX402 { get; set; }
    public bool IsBudgetReceived { get; set; }
}

public class OffChainJob
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("txHash")]
    public string TxHash { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public int ClientId { get; set; }

    [JsonPropertyName("providerId")]
    public int ProviderId { get; set; }

    [JsonPropertyName("budget")]
    public decimal Budget { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public string PublishedAt { get; set; } = string.Empty;

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("clientAddress")]
    public string ClientAddress { get; set; } = string.Empty;

    [JsonPropertyName("providerAddress")]
    public string ProviderAddress { get; set; } = string.Empty;

    [JsonPropertyName("evaluators")]
    public List<string> Evaluators { get; set; } = new();

    [JsonPropertyName("budgetTxHash")]
    public string? BudgetTxHash { get; set; }

    [JsonPropertyName("phase")]
    public int Phase { get; set; }

    [JsonPropertyName("agentIdPair")]
    public string AgentIdPair { get; set; } = string.Empty;

    [JsonPropertyName("onChainJobId")]
    public string OnChainJobId { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("userOpHash")]
    public string? UserOpHash { get; set; }

    [JsonPropertyName("amountClaimed")]
    public decimal AmountClaimed { get; set; }

    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; set; }

    [JsonPropertyName("expiry")]
    public string Expiry { get; set; } = string.Empty;

    [JsonPropertyName("refundRetryTimes")]
    public int RefundRetryTimes { get; set; }

    [JsonPropertyName("additionalFees")]
    public decimal AdditionalFees { get; set; }

    [JsonPropertyName("budgetTokenAddress")]
    public string BudgetTokenAddress { get; set; } = string.Empty;

    [JsonPropertyName("budgetUSD")]
    public decimal BudgetUSD { get; set; }

    [JsonPropertyName("amountClaimedUSD")]
    public decimal? AmountClaimedUSD { get; set; }

    [JsonPropertyName("additionalFeesUSD")]
    public decimal? AdditionalFeesUSD { get; set; }

    [JsonPropertyName("contractAddress")]
    public string ContractAddress { get; set; } = string.Empty;

    [JsonPropertyName("accountId")]
    public int? AccountId { get; set; }

    [JsonPropertyName("x402Nonce")]
    public string X402Nonce { get; set; } = string.Empty;
}

