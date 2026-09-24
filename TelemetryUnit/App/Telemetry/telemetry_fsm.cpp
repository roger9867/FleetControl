#include "telemetry_fsm.hpp"
#include "../Network/interface.h"
#include "../Gnss/interface.h"
#include <cstring>
#include <cstdio>

namespace
{
    constexpr uint32_t PUBLISH_INTERVAL_MS = 1000;
}

TelemetryFsm::TelemetryFsm(Sim7600& _telemetry_module)
    : telemetry_module(_telemetry_module)
{
}

TelemetryState TelemetryFsm::get_state()
{
    return current_state;
}

bool TelemetryFsm::is_network_ready()
{
    return network_fsm_get_state() == NetworkState::NetworkMqttConnected;
}

bool TelemetryFsm::is_gnss_ready()
{
    return gnss_fsm_get_state() == GnssState::GnssReady;
}

bool TelemetryFsm::publish_timestamp()
{
    if (!gnss_fsm_has_valid_fix())
    {
        const char msg[] = "[TELEMETRY FSM] NO GNSS FIX -> skip publish\r\n";
        telemetry_module.huart_debug.write((uint8_t*)msg, strlen(msg));
        return false;
    }

    // GNSS-Daten kommen aus GnssFSM statt eine zweite,
    //  redundante AT+CGNSSINFO-Abfrage pro Publish zu senden.
    telemetry_point.parse_gnss(gnss_fsm_get_last_response());

    const char* json = telemetry_point.to_json();
    telemetry_module.huart_debug.write((uint8_t*)"[TELEMETRY FSM] PUBLISH ", 24);
    telemetry_module.huart_debug.write((uint8_t*)json, strlen(json));
    telemetry_module.huart_debug.write((uint8_t*)"\r\n", 2);

    bool ok = telemetry_module.mqtt_publish(json);

    if (!ok)
    {
        const char msg[] = "[TELEMETRY FSM] MQTT PUBLISH FAILED\r\n";
        telemetry_module.huart_debug.write((uint8_t*)msg, strlen(msg));
    }

    network_fsm_report_publish_result(ok);

    return ok;
}

void TelemetryFsm::step()
{
    switch (current_state)
    {
        // warten, bis Netzwerk und GNSS gleichzeitig bereit sind
        case TelemetryState::Idle:
        {
            if (is_network_ready() && is_gnss_ready())
            {
                const char msg[] = "[TELEMETRY FSM] Idle -> Publishing\r\n";
                telemetry_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                last_publish = 0;
                current_state = TelemetryState::Publishing;
            }
            else
            {
                char msg[80];
                int len = snprintf(msg, sizeof(msg),
                    "[TELEMETRY FSM] waiting (network_ready=%d gnss_ready=%d)\r\n",
                    (int)is_network_ready(), (int)is_gnss_ready());
                telemetry_module.huart_debug.write((uint8_t*)msg, len);
            }
            break;
        }

        // im festen Intervall Telemetriepunkte senden, solange alles bereit bleibt
        case TelemetryState::Publishing:
        {
            if (!is_network_ready() || !is_gnss_ready())
            {
                const char msg[] = "[TELEMETRY FSM] Publishing -> Idle (lost readiness)\r\n";
                telemetry_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                current_state = TelemetryState::Idle;
                break;
            }

            if (HAL_GetTick() - last_publish >= PUBLISH_INTERVAL_MS)
            {
                last_publish = HAL_GetTick();

                if (!publish_timestamp())
                    current_state = TelemetryState::Error;
            }
            break;
        }

        // Fehlerzustand ist nur ein Durchgangspunkt, springt sofort zurück zu Idle
        case TelemetryState::Error:
        {
            const char msg[] = "[TELEMETRY FSM] Error -> Idle\r\n";
            telemetry_module.huart_debug.write((uint8_t*)msg, strlen(msg));

            current_state = TelemetryState::Idle;
            break;
        }
    }
}
