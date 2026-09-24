#pragma once

#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "./network_states.hpp"

// Baut die Mobilfunk- und MQTT-Verbindung auf und überwacht sie
class NetworkFSM
{
    public:
        NetworkState get_state();

        // Einen Schritt der FSM, muss zyklisch aufgerufen werden
        void step();

        // Von TelemetryFsm nach jedem Publish-Versuch aufgerufen, um wiederholte
        // Fehlschlaege als Zeichen einer toten Verbindung zu erkennen
        void report_publish_result(bool ok);

        explicit NetworkFSM(Sim7600& _network_module);

    private:
        NetworkState current_state = NetworkState::NetworkIdle;

        Sim7600& network_module;

        uint32_t last_health_check = 0;
        uint8_t consecutive_publish_failures = 0;
};
