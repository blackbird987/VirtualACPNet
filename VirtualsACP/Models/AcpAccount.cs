using System.Text.Json;
using VirtualsAcp.Blockchain;

namespace VirtualsAcp.Models;

public class AcpAccount
{
    public NethereumBlockchainClient ContractClient { get; }
    public int Id { get; }
    public string ClientAddress { get; }
    public string ProviderAddress { get; }
    public Dictionary<string, object> Metadata { get; }

    public AcpAccount(
        NethereumBlockchainClient contractClient,
        int id,
        string clientAddress,
        string providerAddress,
        Dictionary<string, object> metadata)
    {
        ContractClient = contractClient;
        Id = id;
        ClientAddress = clientAddress;
        ProviderAddress = providerAddress;
        Metadata = metadata;
    }

    public async Task<string> UpdateMetadataAsync(Dictionary<string, object> metadata)
    {
        var metadataJson = JsonSerializer.Serialize(metadata);
        var txHash = await ContractClient.UpdateAccountMetadataAsync(Id, metadataJson);
        return txHash;
    }
}

