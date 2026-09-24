#pragma once

#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "./gnss_states.hpp"



// Aktiviert GNSS und fragt zyklisch den Fix ab
// Cached die Rohantwort, damit TelemetryFsm sie ohne weitere AT-Abfrage weiterverarbeiten kann.
class GnssFSM
{
    public:
        GnssState get_state();

        // Führt Schritt der FSM aus, muss zyklisch aufgerufen werden
        void step();

        // Letzte AT+CGNSSINFO-Rohantwort, von TelemetryFsm zum Parsen genutzt
        const char* get_last_response();
        bool has_valid_fix();

        explicit GnssFSM(Sim7600& _gnss_module);

    private:
        GnssState current_state = GnssState::GnssIdle;

        Sim7600& gnss_module;

        char last_response[160] = {0};
        bool last_fix_valid = false;
};
