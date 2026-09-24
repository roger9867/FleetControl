using System.Collections.Concurrent;
using IngestionService.Logging;
using IngestionService.Models;

namespace IngestionService.Trip;

// Zustandsautomat für Fahrtzustandserkennung pro device_id: (kein Eintrag) = Idle,
//  sonst Connected/Driving/Stopped/ConnectionLost
public class TripReactor : BackgroundService
{
    private static readonly TimeSpan ConnectedTimeout = TimeSpan.FromSeconds(2);

    // Kurze Lücke ohne neue Daten während einer laufenden Fahrt -> Verbindung
    // verloren, ohne die Fahrt sofort zu beenden (Funkloch, etc)
    private static readonly TimeSpan ConnectionLostTimeout = TimeSpan.FromSeconds(2);

    // Bleibt die Verbindung so lange unterbrochen, wird die Fahrt zwangsweise
    // mit dem letzten bekannten Zeitstempel beendet, statt offen zu bleiben
    private static readonly TimeSpan GiveUpTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TimeoutCheckInterval = TimeSpan.FromSeconds(1);

    // 300 aufeinanderfolgende Stillstands-Punkte entsprechen bei einem
    // Datenpunkt pro Sekunde den geforderten 5 Minuten Stillstand, ab denen
    // eine Fahrt als beendet gilt
    private const int StoppedCountThreshold = 60 * 5;

    // Das GNSS-Modul liefert die Geschwindigkeit, mit einer gemessenen Ungenauigkeit
    // von bis zu ±5 km/h, auch im Stand wurde ein Abweichung von
    // + /- 3 km/h beobachtet. Der Schwellwert muss daher darüber liegen
    private const double DrivingSpeedThreshold = 5;

    // Mehrere aufeinanderfolgende Punkte über dem Schwellwert verhindern, dass
    // ein einzelner Ausreißerwert fälschlich eine Fahrt auslöst.
    private const int DrivingCandidateThreshold = 3;

    private readonly ConcurrentDictionary<string, DeviceTripState> _devices = new();
    private readonly object _gate = new();

    private readonly TripClient _tripClient;
    private readonly PendingTripEndQueue _pendingTripEndQueue;
    private readonly ILogger<TripReactor> _logger;


    public TripReactor(
        TripClient tripClient,
        PendingTripEndQueue pendingTripEndQueue,
        ILogger<TripReactor> logger
    ) {
        _tripClient = tripClient;
        _pendingTripEndQueue = pendingTripEndQueue;
        _logger = logger;
    }


    // Wertet einen eingehenden Datenpunkt mithilfe des Zustandsautomaten des Geräts aus,
    // aktualisiert dessen Zustand und löst falls nötig StartTrip/EndTrip per gRPC aus.
    public Task<TripState> DispatchAsync(
        TelemetryEvent data
    ) {
        bool beginTrip = false;
        bool endTrip = false;
        DateTime tripStartTimestamp = default;
        TripState resultState;

        lock (_gate)
        {
            if (!_devices.TryGetValue(data.DeviceId, out var state))
            {
                resultState = TripState.Connected;

                _devices[data.DeviceId] = new DeviceTripState
                {
                    State = TripState.Connected,
                    LastTimestamp = data.Timestamp,
                    DrivingCandidateCount = data.SpeedKmh > DrivingSpeedThreshold ? 1 : 0
                };
            }
            else
            {
                switch (state.State)
                {
                    case TripState.Connected:
                        if (data.SpeedKmh > DrivingSpeedThreshold)
                        {
                            state.DrivingCandidateCount++;

                            if (state.DrivingCandidateCount >= DrivingCandidateThreshold)
                            {
                                state.State = TripState.Driving;
                                state.TripStartTimestamp = data.Timestamp;
                                state.DrivingCandidateCount = 0;
                                beginTrip = true;
                                tripStartTimestamp = data.Timestamp;
                            }
                        }
                        else
                        {
                            state.DrivingCandidateCount = 0;
                        }

                        resultState = state.State;
                        break;

                    case TripState.Driving:
                        if (data.SpeedKmh == 0)
                        {
                            state.State = TripState.Stopped;
                            state.StoppedCount = 1;
                        }

                        resultState = state.State;
                        break;

                    case TripState.Stopped:
                        if (data.SpeedKmh != 0)
                        {
                            state.State = TripState.Driving;
                            state.StoppedCount = 0;
                            resultState = TripState.Driving;
                        }
                        else
                        {
                            state.StoppedCount++;
                            resultState = TripState.Stopped;

                            if (state.StoppedCount >= StoppedCountThreshold)
                            {
                                endTrip = true;
                                tripStartTimestamp = state.TripStartTimestamp;
                                state.State = TripState.Connected;
                                state.StoppedCount = 0;
                            }
                        }
                        break;

                    case TripState.ConnectionLost:
                        if (data.SpeedKmh == 0)
                        {
                            // Eine kurze Verbindungslücke im Stand ist kein Beweis für
                            // Bewegung. StoppedCount läuft weiter statt neu zu starten,
                            // sonst wird der Schwellwert bei wiederkehrenden Lücken nie
                            // erreicht und die Fahrt endet nie automatisch.
                            state.State = TripState.Stopped;
                            state.StoppedCount++;
                            resultState = TripState.Stopped;

                            if (state.StoppedCount >= StoppedCountThreshold)
                            {
                                endTrip = true;
                                tripStartTimestamp = state.TripStartTimestamp;
                                state.State = TripState.Connected;
                                state.StoppedCount = 0;
                                resultState = TripState.Connected;
                            }
                        }
                        else
                        {
                            state.State = TripState.Driving;
                            state.StoppedCount = 0;
                            resultState = TripState.Driving;
                        }

                        break;

                    default:
                        resultState = state.State;
                        break;
                }

                state.LastTimestamp = data.Timestamp;
            }
        }

        _logger.LogHighlighted(
            "Device {deviceId}: State={state}",
            data.DeviceId,
            resultState
        );

        if (beginTrip)
        {
            _ = SafeStartTripAsync(data.DeviceId, tripStartTimestamp);
        }

        if (endTrip)
        {
            _ = SafeEndTripAsync(data.DeviceId, data.Timestamp, tripStartTimestamp);
        }

        return Task.FromResult(resultState);
    }


    // Läuft im Hintergrund und prüft jede Sekiunde alle Geräte auf Timeouts (Idle,
    // Verbindungsverlust, erzwungenes Fahrtende)
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        using var timer = new PeriodicTimer(TimeoutCheckInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckForTimeoutsAsync();
        }
    }


    // Ermittelt für jedes Gerät, ob seit dem letzten Datenpunkt lange genug keine Daten
    // mehr eingetroffen sind und schaltet es auf Idle, ConnectionLost oder erzwungenes Fahrtende
    private Task CheckForTimeoutsAsync()
    {
        var now = DateTime.UtcNow;

        var toIdle = new List<string>();
        var toConnectionLost = new List<string>();
        var toEndTrip = new List<(string DeviceId, DateTime LastTimestamp, DateTime TripStartTimestamp)>();

        lock (_gate)
        {
            foreach (var (deviceId, state) in _devices)
            {
                var idleFor = now - state.LastTimestamp;

                switch (state.State)
                {
                    case TripState.Connected:
                        if (idleFor >= ConnectedTimeout)
                        {
                            toIdle.Add(deviceId);
                        }
                        break;

                    case TripState.Driving:
                    case TripState.Stopped:
                        if (idleFor >= ConnectionLostTimeout)
                        {
                            toConnectionLost.Add(deviceId);
                        }
                        break;

                    case TripState.ConnectionLost:
                        if (idleFor >= GiveUpTimeout)
                        {
                            toEndTrip.Add((deviceId, state.LastTimestamp, state.TripStartTimestamp));
                        }
                        break;
                }
            }

            foreach (var deviceId in toIdle)
            {
                _devices.TryRemove(deviceId, out _);
            }

            foreach (var deviceId in toConnectionLost)
            {
                _devices[deviceId].State = TripState.ConnectionLost;
            }

            foreach (var (deviceId, _, _) in toEndTrip)
            {
                _devices.TryRemove(deviceId, out _);
            }
        }

        foreach (var deviceId in toIdle)
        {
            _logger.LogHighlighted(
                "Device {deviceId}: State=Idle (Timeout)",
                deviceId);
        }

        foreach (var deviceId in toConnectionLost)
        {
            _logger.LogHighlighted(
                "Device {deviceId}: State=ConnectionLost (Timeout)",
                deviceId);
        }

        foreach (var (deviceId, lastTimestamp, tripStartTimestamp) in toEndTrip)
        {
            _logger.LogHighlighted(
                "Device {deviceId}: State=Idle (Timeout, Fahrt beendet)",
                deviceId);

            _ = SafeEndTripAsync(deviceId, lastTimestamp, tripStartTimestamp);
        }

        return Task.CompletedTask;
    }


    // Meldet die erkannte Fahrtanlage per gRPC an FleetControlApi
    private async Task SafeStartTripAsync(
        string deviceId,
        DateTime timestamp
    ) {
        _logger.LogHighlighted(
            "gRPC StartTrip -> deviceId={deviceId}, timestamp={timestamp}",
            deviceId,
            timestamp);

        try
        {
            await _tripClient.StartTripAsync(deviceId, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "StartTrip-Aufruf zur Fahrtanlage fehlgeschlagen für {deviceId}",
                deviceId);
        }
    }


    // Meldet das erkannte Fahrtende per gRPC an FleetControlApi.
    // Schlägt der Aufruf fehl, wird der Eintrag in der PendingTripEndQueue vorgemerkt 
    // und von PendingTripEndRetryService erneut versucht
    private async Task SafeEndTripAsync(
        string deviceId,
        DateTime timestamp,
        DateTime tripStartTimestamp
    ) {
        _logger.LogHighlighted(
            "gRPC EndTrip -> deviceId={deviceId}, timestamp={timestamp}",
            deviceId,
            timestamp);

        try
        {
            await _tripClient.EndTripAsync(deviceId, timestamp, tripStartTimestamp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "EndTrip-Aufruf zur Fahrtanlage fehlgeschlagen für {deviceId}, wird alle 30s erneut versucht",
                deviceId);

            _pendingTripEndQueue.Enqueue(
                new PendingTripEnd(deviceId, tripStartTimestamp, timestamp));
        }
    }
}
