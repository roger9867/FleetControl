#pragma once

#include <cstdint>

#include "./gnss_states.hpp"
#include "../Interfaces/i_clock.hpp"
#include "../Interfaces/i_gnss_modem.hpp"
#include "../Interfaces/i_gnss_source.hpp"
#include "../Interfaces/i_logger.hpp"



// Enables GNSS and periodically queries the fix
// Caches the raw response so that TelemetryFsm can process it without another AT query.
class GnssFSM : public IGnssSource
{
    public:
        GnssState get_state();

        // Executes one step of the FSM, must be called periodically
        void step();

        // IGnssSource
        bool is_ready() const override;
        bool has_valid_fix() const override;
        // Last raw AT+CGNSSINFO response, used by TelemetryFsm for parsing
        const char* last_response() const override;

        GnssFSM(IGnssModem& _modem, IClock& _clock, ILogger& _logger);

    private:
        GnssState current_state = GnssState::GnssIdle;

        // Dependencies, injected via the constructor
        IGnssModem& modem;
        IClock& clock;
        ILogger& logger;

        char last_response_buf[160] = {0};
        bool last_fix_valid = false;

        // Start of the current phase without a fix, for the 60 s timeout
        bool waiting_for_fix = false;
        uint32_t no_fix_start = 0;
};
