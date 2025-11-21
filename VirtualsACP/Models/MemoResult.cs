using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace VirtualsAcp.Models;

[FunctionOutput]
public class MemoResult
{
    [Parameter("tuple[]", "memos", 1)]
    public List<ContractMemo> Memos { get; set; } = new();

    [Parameter("uint256", "total", 2)]
    public BigInteger Total { get; set; }
}

[FunctionOutput]
public class ContractMemo
{
    [Parameter("uint256", "id", 1)]
    public BigInteger Id { get; set; }

    [Parameter("uint256", "jobId", 2)]
    public BigInteger JobId { get; set; }

    [Parameter("address", "sender", 3)]
    public string Sender { get; set; } = string.Empty;

    [Parameter("string", "content", 4)]
    public string Content { get; set; } = string.Empty;

    [Parameter("uint8", "memoType", 5)]
    public int MemoType { get; set; }

    [Parameter("uint256", "createdAt", 6)]
    public BigInteger CreatedAt { get; set; }

    [Parameter("bool", "isApproved", 7)]
    public bool IsApproved { get; set; }

    [Parameter("address", "approvedBy", 8)]
    public string ApprovedBy { get; set; } = string.Empty;

    [Parameter("uint256", "approvedAt", 9)]
    public BigInteger ApprovedAt { get; set; }

    [Parameter("bool", "requiresApproval", 10)]
    public bool RequiresApproval { get; set; }

    [Parameter("string", "metadata", 11)]
    public string Metadata { get; set; } = string.Empty;

    [Parameter("bool", "isSecured", 12)]
    public bool IsSecured { get; set; }

    [Parameter("uint8", "nextPhase", 13)]
    public int NextPhase { get; set; }

    [Parameter("uint256", "expiredAt", 14)]
    public BigInteger ExpiredAt { get; set; }
}

