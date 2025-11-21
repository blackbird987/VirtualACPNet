using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nethereum.ABI.EIP712;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using VirtualsAcp.Abi;
using VirtualsAcp.Configs;
using VirtualsAcp.Exceptions;
using VirtualsAcp.Models;

namespace VirtualsAcp.Services;

public class AcpX402Service
{
    private readonly AcpContractConfig _config;
    private readonly Web3 _web3;
    private readonly Account _account;
    private readonly ILogger? _logger;
    private const int HTTP_STATUS_PAYMENT_REQUIRED = 402;

    public AcpX402Service(
        AcpContractConfig config,
        Web3 web3,
        Account account,
        ILogger? logger = null)
    {
        _config = config;
        _web3 = web3;
        _account = account;
        _logger = logger;
    }

    public async Task<string> SignUpdateJobNonceMessageAsync(int jobId, string nonce)
    {
        try
        {
            var message = $"{jobId}-{nonce}";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var signer = new EthereumMessageSigner();
            var signature = signer.Sign(messageBytes, _account.PrivateKey);

            _logger?.LogInformation("Signed nonce update message for job {JobId}", jobId);
            return signature;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to sign nonce update message for job {JobId}", jobId);
            throw new AcpError("Failed to sign nonce update message", ex);
        }
    }

    public async Task<OffChainJob> UpdateJobNonceAsync(int jobId, string nonce)
    {
        try
        {
            var apiUrl = $"{_config.AcpApiUrl}/jobs/{jobId}/x402-nonce";
            var signature = await SignUpdateJobNonceMessageAsync(jobId, nonce);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-signature", signature);
            httpClient.DefaultRequestHeaders.Add("x-nonce", nonce);
            httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

            var payload = new
            {
                data = new { nonce }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OffChainJob>(responseContent);

            if (result == null)
            {
                throw new AcpApiError("Failed to deserialize job response");
            }

            _logger?.LogInformation("Updated X402 nonce for job {JobId}", jobId);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update job X402 nonce for job {JobId}", jobId);
            throw new AcpError("Failed to update job X402 nonce", ex);
        }
    }

    public async Task<X402Payment> GeneratePaymentAsync(
        X402PayableRequest payableRequest,
        X402PayableRequirements requirements)
    {
        try
        {
            if (requirements.Accepts == null || requirements.Accepts.Count == 0)
            {
                throw new AcpError("No X402 payment requirements found");
            }

            var usdcContract = _config.PaymentTokenAddress;
            var timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var validAfter = timeNow.ToString();
            var validBefore = (timeNow + requirements.Accepts[0].MaxTimeoutSeconds).ToString();

            // Get token name and version
            var tokenContract = _web3.Eth.GetContract(ContractAbis.Erc20Abi, usdcContract);
            var nameFunction = tokenContract.GetFunction("name");
            var tokenName = await nameFunction.CallAsync<string>();

            // For version, we'll need to check if there's a version function
            // For now, we'll use a default or read from config
            var tokenVersion = "2"; // Default for USDC V2

            // Generate random nonce
            var nonceBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonceBytes);
            }
            var nonce = "0x" + Convert.ToHexString(nonceBytes).ToLowerInvariant();

            var message = new X402PaymentMessage
            {
                From = _account.Address,
                To = payableRequest.To,
                Value = ((BigInteger)(payableRequest.Value * (decimal)BigInteger.Pow(10, _config.PaymentTokenDecimals))).ToString(),
                ValidAfter = validAfter,
                ValidBefore = validBefore,
                Nonce = nonce
            };

            // TODO: Implement full EIP-712 typed data signing
            // This requires Nethereum's EIP-712 library which needs proper setup
            // For now, we'll use a placeholder - full implementation needed for production
            // The signature format should match EIP-712 TransferWithAuthorization
            var signer = new EthereumMessageSigner();
            var messageToSign = $"{message.From}{message.To}{message.Value}{message.ValidAfter}{message.ValidBefore}{message.Nonce}";
            var messageBytes = Encoding.UTF8.GetBytes(messageToSign);
            var signature = signer.Sign(messageBytes, _account.PrivateKey);
            
            _logger?.LogWarning("Using simplified signing for X402 payment - full EIP-712 implementation needed");

            var payload = new
            {
                x402Version = requirements.X402Version,
                scheme = requirements.Accepts[0].Scheme,
                network = requirements.Accepts[0].Network,
                payload = new
                {
                    signature,
                    authorization = message
                }
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var encodedPayment = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

            return new X402Payment
            {
                EncodedPayment = encodedPayment,
                Signature = signature,
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate X402 payment");
            throw new AcpError("Failed to generate X402 payment", ex);
        }
    }

    public async Task<X402PaymentResponse> PerformRequestAsync(
        string url,
        string version,
        string? budget = null,
        string? signature = null)
    {
        try
        {
            if (_config.X402Config == null || string.IsNullOrEmpty(_config.X402Config.Url))
            {
                throw new AcpError("X402 URL not configured");
            }

            using var httpClient = new HttpClient();
            
            if (!string.IsNullOrEmpty(signature))
            {
                httpClient.DefaultRequestHeaders.Add("x-payment", signature);
            }
            
            if (!string.IsNullOrEmpty(budget))
            {
                httpClient.DefaultRequestHeaders.Add("x-budget", budget);
            }

            httpClient.DefaultRequestHeaders.Add("x-acp-version", version);

            var fullUrl = $"{_config.X402Config.Url}{url}";
            var response = await httpClient.GetAsync(fullUrl);
            
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<X402PayableRequirements>(content);

            if (data == null)
            {
                throw new AcpApiError("Failed to deserialize X402 response");
            }

            var isPaymentRequired = response.StatusCode == System.Net.HttpStatusCode.PaymentRequired ||
                                    (int)response.StatusCode == HTTP_STATUS_PAYMENT_REQUIRED;

            if (!response.IsSuccessStatusCode && !isPaymentRequired)
            {
                throw new AcpApiError($"Invalid response status code for X402 request: {response.StatusCode}");
            }

            return new X402PaymentResponse
            {
                IsPaymentRequired = isPaymentRequired,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to perform X402 request");
            throw new AcpError("Failed to perform X402 request", ex);
        }
    }
}

