#pragma once

#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "telemetry_point.hpp"

enum class TelemetryState
{
    Idle,       // wartet auf Netzwerk- und GNSS-Bereitschaft
    Publishing, // sendet in festem Intervall Telemetriepunkte per MQTT
    Error       // letzter Publish ist fehlgeschlagen, fällt zurück auf Idle
};


// Steuert das periodische Senden von Telemetriedaten (Position, Geschwindigkeit,
// Beschleunigung), sobald Netzwerk- und GNSS-FSM bereit sind
class TelemetryFsm
{
    public:
        explicit TelemetryFsm(Sim7600& _telemetry_module);

        // Führt einen Schritt der FSM aus, muss zyklisch aufgerufen werden
        void step();
        TelemetryState get_state();

    private:
        TelemetryState current_state = TelemetryState::Idle;

        Sim7600& telemetry_module;
        TelemetryPoint telemetry_point;

        uint32_t last_publish = 0;

        bool is_network_ready();
        bool is_gnss_ready();

        // Baut den aktuellen Telemetriepunkt und sendet ihn per MQTT
        bool publish_timestamp();
};
