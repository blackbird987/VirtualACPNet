using System.Text.Json;
using Microsoft.Extensions.Logging;
using VirtualsAcp.Blockchain;
using VirtualsAcp.Configs;
using VirtualsAcp.Exceptions;
using VirtualsAcp.Http;
using VirtualsAcp.WebSocket;

namespace VirtualsAcp.Models;

public class VirtualsACPClient : IDisposable
{
    private readonly NethereumBlockchainClient _blockchainClient;
    private readonly AcpApiClient _apiClient;
    private readonly ACPSocketIO? socketClient;
    private readonly ILogger? _logger;
    private readonly AcpContractConfig _config;
    private readonly string _agentAddress;

    public event Func<ACPJob, ACPMemo?, Task>? OnNewTask;
    public event Func<ACPJob, ACPMemo?, Task<(bool, string)>>? OnEvaluate;

    public VirtualsACPClient(
        string walletPrivateKey,
        string? agentWalletAddress = null,
        AcpContractConfig? config = null,
        Func<ACPJob, ACPMemo?, Task>? onNewTask = null,
        Func<ACPJob, ACPMemo?, Task<(bool, string)>>? onEvaluate = null,
        ILogger? logger = null)
    {
        _config = config ?? Configurations.DefaultConfig;
        _logger = logger;

        // Initialize blockchain client
        _blockchainClient = new NethereumBlockchainClient(walletPrivateKey, _config, logger, signerAddress: agentWalletAddress);
        _agentAddress = agentWalletAddress ?? _blockchainClient.AgentAddress;

        // Initialize API client
        _apiClient = new AcpApiClient(_config.AcpApiUrl, logger);

        // Initialize SignalR client if callbacks are provided
        if (onNewTask != null || onEvaluate != null)
        {
            socketClient = new ACPSocketIO(_config.AcpApiUrl, _config.ContractAddress, logger, _agentAddress);
            socketClient.OnNewTask += HandleNewTaskAsync;
            socketClient.OnEvaluate += HandleEvaluateAsync;
            OnNewTask = onNewTask;
            OnEvaluate = onEvaluate;
        }
    }

    public string AgentAddress => _agentAddress;
    public string SignerAddress => _blockchainClient.AgentAddress;
    public bool IsConnected => socketClient?.IsConnected ?? false;

    public async Task StartAsync(string? walletAddress = null, string? evaluatorAddress = null)
    {
        if (socketClient != null)
        {
            await socketClient.StartAsync(walletAddress ?? _agentAddress, evaluatorAddress);
        }
    }

    public async Task StopAsync()
    {
        if (socketClient != null)
        {
            await socketClient.StopAsync();
        }
    }

    private async Task HandleNewTaskAsync(object data)
    {
        try
        {
            var jobData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data));
            if (jobData == null) return;

            var memos = new List<ACPMemo>();
            if (jobData.TryGetValue("memos", out var memosData) && memosData is JsonElement memosElement)
            {
                foreach (var memoElement in memosElement.EnumerateArray())
                {
                    var memo = JsonSerializer.Deserialize<ACPMemo>(memoElement.GetRawText());
                    if (memo != null)
                    {
                        memos.Add(memo);
                    }
                }
            }

            var memoToSignId = jobData.TryGetValue("memoToSign", out var memoToSignValue)
                ? JsonSerializer.Deserialize<int?>(memoToSignValue.ToString() ?? "null")
                : null;

            var memoToSign = memoToSignId.HasValue
                ? memos.FirstOrDefault(m => m.Id == memoToSignId.Value)
                : null;

            var context = jobData.TryGetValue("context", out var contextValue)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(contextValue?.ToString() ?? "{}")
                : null;

            var job = new ACPJob
            {
                Id = JsonSerializer.Deserialize<int>(jobData["id"].ToString() ?? "0"),
                ProviderAddress = jobData["providerAddress"].ToString() ?? "",
                ClientAddress = jobData["clientAddress"].ToString() ?? "",
                EvaluatorAddress = jobData["evaluatorAddress"].ToString() ?? "",
                Memos = memos,
                Phase = Enum.Parse<AcpJobPhase>(jobData["phase"].ToString() ?? "Request"),
                Price = JsonSerializer.Deserialize<double>(jobData["price"].ToString() ?? "0"),
                Context = context,
                AcpClient = this
            };

            _logger?.LogInformation("Received new task: {Job}", job.ToString());

            if (OnNewTask != null)
            {
                await OnNewTask(job, memoToSign);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling new task");
        }
    }

    private async Task HandleEvaluateAsync(object data)
    {
        try
        {
            var jobData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data));
            if (jobData == null) return;

            var memos = new List<ACPMemo>();
            if (jobData.TryGetValue("memos", out var memosData) && memosData is JsonElement memosElement)
            {
                foreach (var memoElement in memosElement.EnumerateArray())
                {
                    var memo = JsonSerializer.Deserialize<ACPMemo>(memoElement.GetRawText());
                    if (memo != null)
                    {
                        memos.Add(memo);
                    }
                }
            }

            Dictionary<string, object>? context = null;
            if (jobData.TryGetValue("context", out var contextValue) &&
                contextValue is JsonElement contextElement &&
                contextElement.ValueKind != JsonValueKind.Null)
            {
                context = JsonSerializer.Deserialize<Dictionary<string, object>>(contextElement.GetRawText());
            }

            var job = new ACPJob
            {
                Id = JsonSerializer.Deserialize<int>(jobData["id"].ToString() ?? "0"),
                ProviderAddress = jobData["providerAddress"].ToString() ?? "",
                ClientAddress = jobData["clientAddress"].ToString() ?? "",
                EvaluatorAddress = jobData["evaluatorAddress"].ToString() ?? "",
                Memos = memos,
                Phase = Enum.Parse<AcpJobPhase>(jobData["phase"].ToString() ?? "Request"),
                Price = JsonSerializer.Deserialize<double>(jobData["price"].ToString() ?? "0"),
                Context = context,
                AcpClient = this
            };

            _logger?.LogInformation("Received evaluate: {Job}", job.ToString());

            if (OnEvaluate != null)
            {
                // In self-evaluation scenarios, the memo to sign is DELIVER_SERVICE with nextPhase=4
                var memoToSign = job.Memos.FirstOrDefault(x => x.Type == "DELIVER_SERVICE" && x.NextPhase == AcpJobPhase.Completed);

                if (memoToSign == null)
                {
                    // Fallback: try REQUEST_EVALUATION for non-self-evaluation scenarios
                    memoToSign = job.Memos.FirstOrDefault(x => x.Type == "REQUEST_EVALUATION");
                }

                var (accepted, reason) = await OnEvaluate(job, memoToSign);

                // Sign the correct memo (the one that's PENDING and needs signature)
                var memoIdToSign = memoToSign?.Id ?? job.LatestMemo?.Id ?? 0;

                if (memoIdToSign == 0)
                {
                    _logger?.LogError("No valid memo to sign for job {JobId} evaluation", job.Id);
                    return;
                }

                await SignMemoAsync(memoIdToSign, accepted, reason);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling evaluate");
        }
    }

    public async Task<List<IACPAgent>> BrowseAgentsAsync(
        string keyword,
        string? cluster = null,
        List<AcpAgentSort>? sortBy = null,
        int? topK = null,
        AcpGraduationStatus? graduationStatus = null,
        AcpOnlineStatus? onlineStatus = null)
    {
        var agents = await _apiClient.BrowseAgentsAsync(
            keyword,
            cluster,
            sortBy,
            topK,
            graduationStatus,
            onlineStatus,
            _agentAddress
        );

        // Filter by contract address and exclude current wallet (matching JS behavior)
        var contractAddressLower = _config.ContractAddress.ToLowerInvariant();
        return agents
            .Where(agent => 
                !string.IsNullOrEmpty(agent.WalletAddress) &&
                !agent.WalletAddress.Equals(_agentAddress, StringComparison.OrdinalIgnoreCase) &&
                (!string.IsNullOrEmpty(agent.ContractAddress) &&
                 agent.ContractAddress.Equals(_config.ContractAddress, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<int> InitiateJobAsync(
        string providerAddress,
        object serviceRequirement,
        decimal amount,
        string? evaluatorAddress = null,
        DateTime? expiredAt = null)
    {
        if (expiredAt == null)
            expiredAt = DateTime.UtcNow.AddDays(1);

        var evalAddr = evaluatorAddress ?? _agentAddress;

        if (providerAddress.Equals(_agentAddress, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("You cannot initiate a job with yourself as the provider");

        // Check for existing account between client and provider
        var account = await GetByClientAndProviderAsync(_agentAddress, providerAddress);

        var memoContent = serviceRequirement is string str
            ? str
            : JsonSerializer.Serialize(serviceRequirement);

        string txHash;
        if (account != null)
        {
            // Use existing account to create job
            txHash = await _blockchainClient.CreateJobWithAccountAsync(
                account.Id,
                evalAddr,
                amount,
                _config.PaymentTokenAddress,
                expiredAt.Value);
        }
        else
        {
            // Create new job without account
            txHash = await _blockchainClient.CreateJobAsync(
                providerAddress,
                evalAddr,
                expiredAt.Value,
                _config.PaymentTokenAddress,
                amount,
                memoContent);
        }

        var jobId = await _blockchainClient.GetJobIdFromTransactionAsync(txHash);

        await Task.Delay(2000); // needed for some reason, maybe rpc to slow??

        // Create initial memo
        await _blockchainClient.CreateMemoAsync(
            (int)jobId,
            memoContent,
            MemoType.Message,
            true,
            AcpJobPhase.Negotiation
        );

        _logger?.LogInformation("Initial memo for job {JobId} created", jobId);

        // Notify API
        var payload = new Dictionary<string, object>
        {
            ["jobId"] = (int)jobId,
            ["clientAddress"] = _agentAddress,
            ["providerAddress"] = providerAddress,
            ["description"] = serviceRequirement,
            ["expiredAt"] = expiredAt.Value.ToUniversalTime().ToString("O"),
            ["evaluatorAddress"] = evaluatorAddress ?? "",
            ["price"] = amount
        };

        //await _apiClient.CreateJobAsync(payload);

        return (int)jobId;
    }

    public async Task<string> RespondToJobAsync(
        int jobId,
        int memoId,
        bool accept,
        string? content,
        string? reason = "")
    {
        try
        {
            var txHash = await _blockchainClient.SignMemoAsync(memoId, accept, reason ?? "");

            if (!accept)
                return txHash;

            _logger?.LogInformation("Responding to job {JobId} with memo {MemoId} and accept {Accept} and reason {Reason}",
                jobId, memoId, accept, reason);

            await Task.Delay(5000); // virtual backend needs this, has issues sending event

            await _blockchainClient.CreateMemoAsync(
                jobId,
                content ?? $"Job {jobId} accepted. {reason ?? ""}",
                MemoType.Message,
                false,
                AcpJobPhase.Transaction
            );

            _logger?.LogInformation("Responded to job {JobId} with memo {MemoId} and accept {Accept} and reason {Reason}",
                jobId, memoId, accept, reason);

            return txHash;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in respond_to_job_memo");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> PayJobAsync(
        int jobId,
        int memoId,
        decimal amount,
        string? reason = "")
    {
        await _blockchainClient.ApproveAllowanceAsync(amount);

        await Task.Delay(5000);

        var txHash = await _blockchainClient.SignMemoAsync(memoId, true, reason ?? "");

        _logger?.LogInformation("Paid for job {JobId} with memo {MemoId} and amount {Amount}",
            jobId, memoId, amount);

        // ✅ CRITICAL: Create EVALUATION trigger memo to transition job phase
        // This tells the seller that payment is complete and they should deliver
        // Reference: Official Python SDK's pay_and_accept_requirement() does this
        await Task.Delay(5000); // Allow payment memo to settle

        await _blockchainClient.CreateMemoAsync(
            jobId,
            $"Payment made. {reason ?? ""}".Trim(),
            MemoType.Message,
            true,
            AcpJobPhase.Evaluation // ← Transitions job to EVALUATION phase, triggers seller
        );

        _logger?.LogInformation("Created EVALUATION trigger memo for job {JobId}", jobId);

        return new Dictionary<string, object> { ["txHash"] = txHash };
    }

    public async Task<string> RequestFundsAsync(
        int jobId,
        decimal amount,
        string receiverAddress,
        decimal feeAmount,
        FeeType feeType,
        GenericPayload reason,
        AcpJobPhase nextPhase,
        DateTime expiredAt)
    {
        var txHash = await _blockchainClient.CreatePayableMemoAsync(
            jobId,
            JsonSerializer.Serialize(reason),
            amount,
            receiverAddress,
            feeAmount,
            feeType,
            MemoType.PayableRequest,
            expiredAt,
            false,
            nextPhase
        );

        return txHash;
    }

    public async Task<string> RespondToFundsRequestAsync(
        int memoId,
        bool accept,
        decimal amount,
        string? reason = "")
    {
        if (!accept)
        {
            var txHash = await _blockchainClient.SignMemoAsync(memoId, false, reason ?? "");
            return txHash;
        }

        if (amount > 0)
        {
            await _blockchainClient.ApproveAllowanceAsync(amount);
        }

        var signTxHash = await _blockchainClient.SignMemoAsync(memoId, true, reason ?? "");
        return signTxHash;
    }

    public async Task<string> TransferFundsAsync(
        int jobId,
        decimal amount,
        string receiverAddress,
        decimal feeAmount,
        FeeType feeType,
        GenericPayload reason,
        AcpJobPhase nextPhase,
        DateTime expiredAt)
    {
        var totalAmount = amount + feeAmount;

        if (totalAmount > 0)
        {
            await _blockchainClient.ApproveAllowanceAsync(totalAmount);
        }

        var txHash = await _blockchainClient.CreatePayableMemoAsync(
            jobId,
            JsonSerializer.Serialize(reason),
            amount,
            receiverAddress,
            feeAmount,
            feeType,
            MemoType.PayableTransferEscrow,
            expiredAt,
            false,
            nextPhase
        );

        _logger?.LogInformation(
            "Funds transferred for job {JobId} with amount {Amount} to {ReceiverAddress} and reason {Reason}, tx_hash: {TxHash}",
            jobId, amount, receiverAddress, reason, txHash);

        return txHash;
    }

    public async Task<string> SendMessageAsync(
        int jobId,
        GenericPayload message,
        AcpJobPhase nextPhase)
    {
        var txHash = await _blockchainClient.CreateMemoAsync(
            jobId,
            JsonSerializer.Serialize(message),
            MemoType.Message,
            false,
            nextPhase
        );

        return txHash;
    }

    public async Task<string> RespondToFundsTransferAsync(
        int memoId,
        bool accept,
        string? reason = "")
    {
        var txHash = await _blockchainClient.SignMemoAsync(memoId, accept, reason ?? "");
        return txHash;
    }

    public async Task<string> DeliverJobAsync(int jobId, IDeliverable deliverable)
    {
        var deliverableJson = JsonSerializer.Serialize(deliverable);
        var txHash = await _blockchainClient.CreateMemoAsync(
            jobId,
            deliverableJson,
            MemoType.ObjectUrl,
            true,
            AcpJobPhase.Completed  // ✅ CORRECT: Creates memo with nextPhase=4, job completes after buyer approval (matches official SDK)
        );

        _logger?.LogInformation("Delivered job {JobId} with nextPhase=Completed", jobId);
        return txHash;
    }

    public async Task<string> SignMemoAsync(int memoId, bool accept, string? reason = "")
    {
        var txHash = await _blockchainClient.SignMemoAsync(memoId, accept, reason ?? "");
        _logger?.LogInformation("Signed memo for memo ID {MemoId} is {Status}, tx_hash: {TxHash}",
            memoId, accept ? "accepted" : "rejected", txHash);
        return txHash;
    }

    public async Task<List<ACPJob>> GetActiveJobsAsync(int page = 1, int pageSize = 10)
    {
        return await _apiClient.GetActiveJobsAsync(_agentAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCompletedJobsAsync(int page = 1, int pageSize = 10)
    {
        return await _apiClient.GetCompletedJobsAsync(_agentAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCancelledJobsAsync(int page = 1, int pageSize = 10)
    {
        return await _apiClient.GetCancelledJobsAsync(_agentAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetPendingMemoJobsAsync(int page = 1, int pageSize = 10)
    {
        return await _apiClient.GetPendingMemoJobsAsync(_agentAddress, page, pageSize);
    }

    public async Task<ACPJob?> GetJobByIdAsync(int jobId)
    {
        return await _apiClient.GetJobByIdAsync(jobId, _agentAddress);
    }

    public async Task<ACPMemo?> GetMemoByIdAsync(int jobId, int memoId)
    {
        return await _apiClient.GetMemoByIdAsync(jobId, memoId, _agentAddress);
    }

    public async Task<IACPAgent?> GetAgentAsync(string walletAddress)
    {
        return await _apiClient.GetAgentAsync(walletAddress);
    }

    public async Task<AcpAccount?> GetAccountByJobIdAsync(int jobId)
    {
        var accountData = await _apiClient.GetAccountByJobIdAsync(jobId);
        if (accountData == null)
        {
            return null;
        }

        var metadata = accountData.Metadata ?? new Dictionary<string, object>();
        return new AcpAccount(
            _blockchainClient,
            accountData.Id,
            accountData.ClientAddress,
            accountData.ProviderAddress,
            metadata);
    }

    public async Task<AcpAccount?> GetByClientAndProviderAsync(string clientAddress, string providerAddress)
    {
        var accountData = await _apiClient.GetAccountByClientAndProviderAsync(clientAddress, providerAddress);
        if (accountData == null)
        {
            return null;
        }

        var metadata = accountData.Metadata ?? new Dictionary<string, object>();
        return new AcpAccount(
            _blockchainClient,
            accountData.Id,
            accountData.ClientAddress,
            accountData.ProviderAddress,
            metadata);
    }

    public async Task<string> CreateAccountAsync(string providerAddress, Dictionary<string, object> metadata)
    {
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        var txHash = await _blockchainClient.CreateAccountAsync(providerAddress, metadataJson);
        _logger?.LogInformation("Account creation transaction sent: {TxHash}", txHash);
        return txHash;
    }

    public void Dispose()
    {
        _blockchainClient?.Dispose();
        _apiClient?.Dispose();
        socketClient?.Dispose();
    }
}
