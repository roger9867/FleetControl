using FleetControlServer.Service;
using Microsoft.AspNetCore.SignalR;

namespace FleetControlServer.Api.Realtime;

public class UsbHotplugWatcher : BackgroundService
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<VehicleHub> _hub;
    private readonly ILogger<UsbHotplugWatcher> _logger;

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _debounceCts;
    private readonly SemaphoreSlim _broadcastLock = new(1, 1);
    private volatile bool _pendingRerun;

    public UsbHotplugWatcher(
        IServiceScopeFactory scopeFactory,
        IHubContext<VehicleHub> hub,
        ILogger<UsbHotplugWatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists("/dev"))
        {
            _logger.LogWarning("USB-Hotplug-Watcher: /dev nicht gefunden, deaktiviert");
            return;
        }

        _watcher = new FileSystemWatcher("/dev")
        {
            NotifyFilter = NotifyFilters.FileName
        };

        _watcher.Created += (_, e) => OnDeviceChanged(e.Name);
        _watcher.Deleted += (_, e) => OnDeviceChanged(e.Name);
        _watcher.EnableRaisingEvents = true;

        _logger.LogInformation("USB-Hotplug-Watcher gestartet (/dev)");

        await BroadcastAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _watcher.Dispose();
        }
    }

    private void OnDeviceChanged(string? deviceName)
    {
        if (deviceName == null) return;
        if (!deviceName.StartsWith("ttyUSB") && !deviceName.StartsWith("ttyACM")) return;

        _debounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _debounceCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, cts.Token);
                await BroadcastAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async Task BroadcastAsync(CancellationToken token)
    {
        if (!await _broadcastLock.WaitAsync(0, token))
        {
            _pendingRerun = true;
            return;
        }

        try
        {
            do
            {
                _pendingRerun = false;
                await RunBroadcastAsync(token);
            } while (_pendingRerun && !token.IsCancellationRequested);
        }
        finally
        {
            _broadcastLock.Release();
        }
    }

    private async Task RunBroadcastAsync(CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();
        var telemetryUnitService = scope.ServiceProvider.GetRequiredService<TelemetryUnitService>();

        Dictionary<string, string?> responses;

        try
        {
            responses = await telemetryUnitService.BroadcastCommandAsync("get_device_id");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "USB-Broadcast nach Hotplug-Event fehlgeschlagen");
            return;
        }

        var uuids = responses.Values
            .Where(v => !string.IsNullOrWhiteSpace(v) && Guid.TryParse(v, out _))
            .Select(v => v!.Trim())
            .Distinct()
            .ToList();

        await _hub.Clients.All.SendAsync("UsbUnitsChanged", uuids, token);
    }
}
