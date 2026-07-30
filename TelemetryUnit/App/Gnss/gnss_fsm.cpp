#include "gnss_fsm.hpp"
//#include ""

GnssState GnssFSM::get_state()
{
    return current_state;
}


GnssFSM::GnssFSM(Sim7600& _gnss_module)
    : gnss_module(_gnss_module)
{
}

const char* GnssFSM::get_last_response()
{
    return last_response;
}

bool GnssFSM::has_valid_fix()
{
    return last_fix_valid;
}


void GnssFSM::step()
{
    switch(current_state)
    {
        case GnssState::GnssIdle:
        {
            const char msg[] = "[GNSS FSM] IDLE -> ENABLE GNSS\r\n";
            gnss_module.huart_debug.write(
                (uint8_t*)msg,
                sizeof(msg) - 1
            );

            gnss_module.activate_gnss();
            current_state = GnssState::GnssEnabling;
            break;
        }

        case GnssState::GnssEnabling:
        {
            const char msg[] = "[GNSS FSM] START NMEA\r\n";
            gnss_module.huart_debug.write(
                (uint8_t*)msg,
                sizeof(msg) - 1
            );

            gnss_module.gnss_nmea_start();
            current_state = GnssState::GnssReady;
            break;
        }

case GnssState::GnssReady:
{
    const char msg[] =
        "[GNSS FSM] CHECK FIX\r\n";

    gnss_module.huart_debug.write(
        (uint8_t*)msg,
        sizeof(msg) - 1
    );

    // Prüfen ob GNSS Fix vorhanden (und Rohantwort für TelemetryFsm cachen)
    bool fix = gnss_module.get_gnss_fix((uint8_t*)last_response, sizeof(last_response));
    last_fix_valid = fix;

    // Timeout
    static uint32_t no_fix_start = 0;

    if (fix)
    {
        // Timer zurücksetzen
        no_fix_start = 0;

        const char ok[] =
            "[GNSS FSM] FIX OK\r\n";

        gnss_module.huart_debug.write(
            (uint8_t*)ok,
            sizeof(ok) - 1
        );
    }
    else
    {
        const char wait[] =
            "[GNSS FSM] WAITING FOR FIX\r\n";

        gnss_module.huart_debug.write(
            (uint8_t*)wait,
            sizeof(wait) - 1
        );

        // erste Zeit merken
        if (no_fix_start == 0)
        {
            no_fix_start = HAL_GetTick();
        }

        // länger als 60 Sekunden kein Fix
        if ((HAL_GetTick() - no_fix_start) >= 60000)
        {
            const char err[] =
                "[GNSS FSM] NO FIX 60s -> ERROR\r\n";

            gnss_module.huart_debug.write(
                (uint8_t*)err,
                sizeof(err) - 1
            );

            no_fix_start = 0;
            last_fix_valid = false;

            current_state = GnssState::GnssError;
        }
    }

    break;
}

        case GnssState::GnssError:
        {
            const char msg[] =
                "[GNSS FSM] RECOVERY\r\n";

            gnss_module.huart_debug.write(
                (uint8_t*)msg,
                sizeof(msg) - 1
            );

            // GNSS erneut starten
            gnss_module.activate_gnss();

            //osDelay(1000);

            gnss_module.gnss_nmea_start();

            current_state = GnssState::GnssReady;

            break;
        }
    }
}