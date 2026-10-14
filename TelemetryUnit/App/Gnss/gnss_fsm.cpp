#include "gnss_fsm.hpp"

GnssState GnssFSM::get_state()
{
    return current_state;
}


GnssFSM::GnssFSM(IGnssModem& _modem, IClock& _clock, ILogger& _logger)
    : modem(_modem),
      clock(_clock),
      logger(_logger)
{
}

const char* GnssFSM::last_response() const
{
    return last_response_buf;
}

bool GnssFSM::has_valid_fix() const
{
    return last_fix_valid;
}

bool GnssFSM::is_ready() const
{
    return current_state == GnssState::GnssReady; 
}


void GnssFSM::step()
{
    switch(current_state)
    {
        // Switch on the GNSS subsystem
        case GnssState::GnssIdle:
        {
            logger.log("[GNSS FSM] IDLE -> ENABLE GNSS");

            modem.activate_gnss();
            current_state = GnssState::GnssEnabling;
            break;
        }

        // Start NMEA position output
        case GnssState::GnssEnabling:
        {
            logger.log("[GNSS FSM] START NMEA");

            modem.gnss_nmea_start();
            current_state = GnssState::GnssReady;
            break;
        }

        // periodically check whether a GNSS fix is available
        case GnssState::GnssReady:
        {
            logger.log("[GNSS FSM] CHECK FIX");

            // Check whether a GNSS fix is available (and cache the raw response for TelemetryFsm)
            bool fix = modem.get_gnss_fix((uint8_t*)last_response_buf, sizeof(last_response_buf));
            last_fix_valid = fix;

            if (fix)
            {
                // Reset timer
                waiting_for_fix = false;
                no_fix_start = 0;

                logger.log("[GNSS FSM] FIX OK");
            }
            else
            {
                logger.log("[GNSS FSM] WAITING FOR FIX");

                // remember the first time
                if (!waiting_for_fix)
                {
                    waiting_for_fix = true;
                    no_fix_start = clock.get_clock_ticks();
                }

                // no fix for more than 60 seconds
                if ((clock.get_clock_ticks() - no_fix_start) >= 60000)
                {
                    logger.log("[GNSS FSM] NO FIX 60s -> ERROR");

                    waiting_for_fix = false;
                    no_fix_start = 0;
                    last_fix_valid = false;

                    current_state = GnssState::GnssError;
                }
            }

            break;
        }

        // Re-initialize GNSS after timeout
        case GnssState::GnssError:
        {
            logger.log("[GNSS FSM] RECOVERY");

            // Restart GNSS
            modem.activate_gnss();
            modem.gnss_nmea_start();

            current_state = GnssState::GnssReady;

            break;
        }
    }
}
