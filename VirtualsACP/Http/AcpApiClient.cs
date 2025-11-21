using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VirtualsAcp.Exceptions;
using VirtualsAcp.Models;

namespace VirtualsAcp.Http;

public class AcpApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly string _baseUrl;

    public AcpApiClient(string baseUrl, ILogger? logger = null)
    {
        _baseUrl = baseUrl;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private async Task<T?> SendRequestAsync<T>(
        string url,
        Dictionary<string, string>? headers = null,
        string errorContext = "request") where T : class
    {
        try
        {
            _logger?.LogDebug("Making request to: {Url}", url);

            HttpRequestMessage request;
            if (headers != null && headers.Any())
            {
                request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Get, url);
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<T>>(content);

            if (result?.Error != null)
            {
                throw new AcpApiError(result.Error.Message);
            }

            return result?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error during {ErrorContext}", errorContext);
            throw new AcpApiError($"Failed: {errorContext}", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during {ErrorContext}", errorContext);
            throw new AcpError($"An unexpected error occurred: {errorContext}", ex);
        }
    }

    public async Task<List<IACPAgent>> BrowseAgentsAsync(
        string keyword,
        string? cluster = null,
        List<AcpAgentSort>? sortBy = null,
        int? topK = null,
        AcpGraduationStatus? graduationStatus = null,
        AcpOnlineStatus? onlineStatus = null,
        string? walletAddressToExclude = null)
    {
        var url = $"{_baseUrl}/agents/v3/search?search={Uri.EscapeDataString(keyword)}";
        
        if (sortBy != null && sortBy.Any())
        {
            var sortValues = string.Join(",", sortBy.Select(s => s.ToString()));
            url += $"&sortBy={Uri.EscapeDataString(sortValues)}";
        }

        if (topK.HasValue)
        {
            url += $"&top_k={topK.Value}";
        }

        if (!string.IsNullOrEmpty(walletAddressToExclude))
        {
            url += $"&walletAddressesToExclude={Uri.EscapeDataString(walletAddressToExclude)}";
        }

        if (!string.IsNullOrEmpty(cluster))
        {
            url += $"&cluster={Uri.EscapeDataString(cluster)}";
        }

        if (graduationStatus.HasValue)
        {
            url += $"&graduationStatus={graduationStatus.Value.ToString().ToLowerInvariant()}";
        }

        if (onlineStatus.HasValue)
        {
            url += $"&onlineStatus={onlineStatus.Value.ToString().ToLowerInvariant()}";
        }

        var result = await SendRequestAsync<List<IACPAgent>>(url, null, "browsing agents");
        return result ?? new List<IACPAgent>();
    }

    public async Task<List<ACPJob>> GetActiveJobsAsync(string walletAddress, int page = 1, int pageSize = 10)
    {
        return await GetJobsAsync("active", walletAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCompletedJobsAsync(string walletAddress, int page = 1, int pageSize = 10)
    {
        return await GetJobsAsync("completed", walletAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCancelledJobsAsync(string walletAddress, int page = 1, int pageSize = 10)
    {
        return await GetJobsAsync("cancelled", walletAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetPendingMemoJobsAsync(string walletAddress, int page = 1, int pageSize = 10)
    {
        var url = $"{_baseUrl}/jobs/pending-memos?pagination[page]={page}&pagination[pageSize]={pageSize}";
        var headers = new Dictionary<string, string> { ["wallet-address"] = walletAddress };
        
        var result = await SendRequestAsync<List<ACPJob>>(url, headers, "getting pending memo jobs");
        return result ?? new List<ACPJob>();
    }

    private async Task<List<ACPJob>> GetJobsAsync(string jobType, string walletAddress, int page, int pageSize)
    {
        var url = $"{_baseUrl}/jobs/{jobType}?pagination[page]={page}&pagination[pageSize]={pageSize}";
        var headers = new Dictionary<string, string> { ["wallet-address"] = walletAddress };
        
        var result = await SendRequestAsync<List<ACPJob>>(url, headers, $"getting {jobType} jobs");
        return result ?? new List<ACPJob>();
    }

    public async Task<ACPJob?> GetJobByIdAsync(int jobId, string walletAddress)
    {
        var url = $"{_baseUrl}/jobs/{jobId}";
        var headers = new Dictionary<string, string> { ["wallet-address"] = walletAddress };
        
        return await SendRequestAsync<ACPJob>(url, headers, $"getting job {jobId}");
    }

    public async Task<ACPMemo?> GetMemoByIdAsync(int jobId, int memoId, string walletAddress)
    {
        var url = $"{_baseUrl}/jobs/{jobId}/memos/{memoId}";
        var headers = new Dictionary<string, string> { ["wallet-address"] = walletAddress };
        
        return await SendRequestAsync<ACPMemo>(url, headers, $"getting memo {memoId} for job {jobId}");
    }

    public async Task<IACPAgent?> GetAgentAsync(string walletAddress)
    {
        var url = $"{_baseUrl}/agents?filters[walletAddress]={Uri.EscapeDataString(walletAddress)}";
        
        var result = await SendRequestAsync<List<IACPAgent>>(url, null, $"getting agent {walletAddress}");
        return result?.FirstOrDefault();
    }

    public async Task<AcpAccountData?> GetAccountByJobIdAsync(int jobId)
    {
        var url = $"{_baseUrl}/accounts/job/{jobId}";
        return await SendRequestAsync<AcpAccountData>(url, null, $"getting account by job ID {jobId}");
    }

    public async Task<AcpAccountData?> GetAccountByClientAndProviderAsync(string clientAddress, string providerAddress)
    {
        var url = $"{_baseUrl}/accounts/client/{Uri.EscapeDataString(clientAddress)}/provider/{Uri.EscapeDataString(providerAddress)}";
        return await SendRequestAsync<AcpAccountData>(url, null, $"getting account by client {clientAddress} and provider {providerAddress}");
    }

    public async Task<OffChainJob> UpdateJobX402NonceAsync(int jobId, string nonce, string signature)
    {
        try
        {
            var url = $"{_baseUrl}/jobs/{jobId}/x402-nonce";

            _logger?.LogDebug("Making request to: {Url}", url);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-signature", signature);
            request.Headers.Add("x-nonce", nonce);
            request.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { data = new { nonce } }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<OffChainJob>(content);

            if (result == null)
            {
                throw new AcpApiError($"Failed to deserialize response for job {jobId}");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while updating X402 nonce for job {JobId}", jobId);
            throw new AcpApiError($"Failed to update X402 nonce for job: {jobId}", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while updating X402 nonce for job {JobId}", jobId);
            throw new AcpError($"An unexpected error occurred while updating X402 nonce for job: {jobId}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private class ApiResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }
    }

    private class ApiError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

