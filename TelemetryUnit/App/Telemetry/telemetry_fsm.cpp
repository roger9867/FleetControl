#include "telemetry_fsm.hpp"


TelemetryFsm::TelemetryFsm(IMqttPublisher& _mqtt,
                           INetworkStatus& _network,
                           IGnssSource& _gnss,
                           TelemetryPoint& _telemetry_point,
                           IClock& _clock,
                           ILogger& _logger)
    : mqtt(_mqtt),
      network(_network),
      gnss(_gnss),
      telemetry_point(_telemetry_point),
      clock(_clock),
      logger(_logger)
{
}

TelemetryState TelemetryFsm::get_state()
{
    return current_state;
}

bool TelemetryFsm::is_network_ready()
{
    return network.is_mqtt_connected();
}

bool TelemetryFsm::is_gnss_ready()
{
    return gnss.is_ready();
}

bool TelemetryFsm::publish_timestamp()
{
    if (!gnss.has_valid_fix())
    {
        logger.log("[TELEMETRY FSM] NO GNSS FIX -> skip publish");
        return false;
    }

    // GNSS data comes from GnssFSM instead of sending a second,
    //  redundant AT+CGNSSINFO query per publish.
    telemetry_point.parse_gnss(gnss.last_response());

    const char* json = telemetry_point.to_json();
    logger.logf("[TELEMETRY FSM] PUBLISH %s", json);

    bool ok = mqtt.publish(json);

    if (!ok)
    {
        logger.log("[TELEMETRY FSM] MQTT PUBLISH FAILED");
    }

    network.report_publish_result(ok);

    return ok;
}

void TelemetryFsm::step()
{
    switch (current_state)
    {
        // wait until network and GNSS are ready at the same time
        case TelemetryState::Idle:
        {
            if (is_network_ready() && is_gnss_ready())
            {
                logger.log("[TELEMETRY FSM] Idle -> Publishing");

                last_publish = 0;
                current_state = TelemetryState::Publishing;
            }
            else
            {
                logger.logf("[TELEMETRY FSM] waiting (network_ready=%d gnss_ready=%d)",
                    (int)is_network_ready(), (int)is_gnss_ready());
            }
            break;
        }

        // send telemetry points at a fixed interval as long as everything stays ready
        case TelemetryState::Publishing:
        {
            if (!is_network_ready() || !is_gnss_ready())
            {
                logger.log("[TELEMETRY FSM] Publishing -> Idle (lost readiness)");

                current_state = TelemetryState::Idle;
                break;
            }

            if (clock.get_clock_ticks() - last_publish >= PUBLISH_INTERVAL_MS)
            {
                last_publish = clock.get_clock_ticks();

                if (!publish_timestamp())
                    current_state = TelemetryState::Error;
            }
            break;
        }

        // The error state is only a transit state, jumps straight back to Idle
        case TelemetryState::Error:
        {
            logger.log("[TELEMETRY FSM] Error -> Idle");

            current_state = TelemetryState::Idle;
            break;
        }
    }
}
