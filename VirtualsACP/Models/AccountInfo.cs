using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace VirtualsAcp.Models;

[FunctionOutput]
public class AccountInfo
{
    [Parameter("uint256", "id", 1)]
    public BigInteger Id { get; set; }

    [Parameter("address", "client", 2)]
    public string Client { get; set; } = string.Empty;

    [Parameter("address", "provider", 3)]
    public string Provider { get; set; } = string.Empty;

    [Parameter("uint256", "createdAt", 4)]
    public BigInteger CreatedAt { get; set; }

    [Parameter("string", "metadata", 5)]
    public string Metadata { get; set; } = string.Empty;

    [Parameter("uint256", "jobCount", 6)]
    public BigInteger JobCount { get; set; }

    [Parameter("uint256", "completedJobCount", 7)]
    public BigInteger CompletedJobCount { get; set; }

    [Parameter("bool", "isActive", 8)]
    public bool IsActive { get; set; }
}

