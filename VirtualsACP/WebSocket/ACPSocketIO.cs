using Microsoft.Extensions.Logging;
using SocketIOClient;
using System.Text.Json;

namespace VirtualsAcp.WebSocket;

public class ACPSocketIO : IDisposable
{
    private readonly SocketIOClient.SocketIO _client;
    private readonly ILogger? _logger;
    private bool _disposed = false;

    public event Func<object, Task>? OnRoomJoined;
    public event Func<object, Task>? OnEvaluate;
    public event Func<object, Task>? OnNewTask;

    public ACPSocketIO(string socketUrl, string contract, ILogger? logger = null, string agent = null)
    {
        _logger = logger;

        // Convert HTTP API URL to Socket.IO URL if needed
        var socketIOUrl = ConvertToSocketIOUrl(socketUrl);

        _client = new SocketIOClient.SocketIO(socketIOUrl, new SocketIOOptions
        {
            // Use WebSocket transport
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,

            // Enable automatic reconnection
            Reconnection = true,
            ReconnectionAttempts = int.MaxValue,
            ReconnectionDelay = 1000,
            RandomizationFactor = 0.5,
            ReconnectionDelayMax = 5000,
            ConnectionTimeout = TimeSpan.FromSeconds(10),

            ExtraHeaders = new Dictionary<string, string>
            {
                ["x-sdk-version"] = VirtualsAcp.Version,
                ["x-sdk-language"] = "csharp",
                ["x-contract-address"] = contract
            },           

            // Enable all transports as fallback
            AutoUpgrade = true
        });

        SetupEventHandlers();
        SetupConnectionEvents();
    }

    private string ConvertToSocketIOUrl(string apiUrl)
    {
        // Convert from API URL to Socket.IO URL
        // From: https://acpx.virtuals.io/api
        // To: https://acpx.virtuals.io (remove /api suffix)
        return apiUrl.Replace("/api", string.Empty);
    }

    private void SetupEventHandlers()
    {
        _client.On("roomJoined", response =>
        {
            var data = response.GetValue<object>();
            _logger?.LogInformation("Connected to room: {Data}", JsonSerializer.Serialize(data));

            if (OnRoomJoined != null)
            {
                SafeRun(() => OnRoomJoined(data), "OnRoomJoined");
            }
        });

        _client.On("onEvaluate", response =>
        {
            var data = response.GetValue<object>();

            if (OnEvaluate != null)
            {
                SafeRun(() => OnEvaluate(data), "OnEvaluate");
            }
            else
            {
                _logger?.LogWarning("OnEvaluate callback is null, not triggering");
            }
        });

        _client.On("onNewTask", response =>
        {
            var data = response.GetValue<object>();

            if (OnNewTask != null)
            {
                SafeRun(() => OnNewTask(data), "OnNewTask");
            }
            else
            {
                _logger?.LogWarning("OnNewTask callback is null, not triggering");
            }
        });
    }

    private void SetupConnectionEvents()
    {
        _client.OnConnected += (sender, e) =>
        {
            _logger?.LogInformation("Socket.IO connection established");
        };

        _client.OnDisconnected += (sender, e) =>
        {
            _logger?.LogWarning("Socket.IO disconnected: {Reason}", e);
        };

        _client.OnReconnectAttempt += (sender, e) =>
        {
            _logger?.LogInformation("Socket.IO reconnection attempt: {Attempt}", e);
        };

        _client.OnReconnected += (sender, e) =>
        {
            _logger?.LogInformation("Socket.IO reconnected after {Attempt} attempts", e);
        };

        _client.OnError += (sender, e) =>
        {
            _logger?.LogError("Socket.IO error: {Error}", e);
        };
    }

    public async Task StartAsync(string walletAddress, string? evaluatorAddress = null)
    {
        try
        {
            // Set authentication data (equivalent to auth_data in Python)
            var authData = new Dictionary<string, object>
            {
                ["walletAddress"] = walletAddress
            };

            if (!string.IsNullOrEmpty(evaluatorAddress))
            {
                authData["evaluatorAddress"] = evaluatorAddress;
                _logger?.LogInformation("━━━ Registering as evaluator: {EvaluatorAddress} ━━━", evaluatorAddress);
            }
            else
            {
                _logger?.LogWarning("No evaluatorAddress provided - onEvaluate events may not fire");
            }

            // Set auth data before connecting
            _client.Options.Auth = authData;

            // Connect to the Socket.IO server
            await _client.ConnectAsync();
            _logger?.LogInformation("Socket.IO connection started for wallet: {WalletAddress}", walletAddress);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start Socket.IO connection");
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_client.Connected)
            {
                await _client.DisconnectAsync();
            }
            _logger?.LogInformation("Socket.IO connection stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping Socket.IO connection");
        }
    }   

    public void SafeRun(Func<Task> action, string operationName = "async operation")
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error during {operationName}: {ex.Message}");
            }
        });
    }

    public bool IsConnected => _client?.Connected ?? false;

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (_client?.Connected == true)
                {
                    _client.DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
                }
                _client?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing Socket.IO client");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}