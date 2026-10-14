#include "network_fsm.hpp"

namespace
{
    constexpr uint32_t HEALTH_CHECK_INTERVAL_MS = 5000;
    constexpr uint8_t MAX_CONSECUTIVE_PUBLISH_FAILURES = 3;
}

NetworkFSM::NetworkFSM(INetworkModem& _modem, IClock& _clock, ILogger& _logger)
    : modem(_modem),
      clock(_clock),
      logger(_logger)
{
}

NetworkState NetworkFSM::get_state()
{
    return current_state;
}

bool NetworkFSM::is_mqtt_connected() const
{ 
    return current_state == NetworkState::NetworkMqttConnected;
}

void NetworkFSM::report_publish_result(bool ok)
{
    consecutive_publish_failures = ok ? 0 : (consecutive_publish_failures + 1);
}

void NetworkFSM::step()
{
    switch (current_state)
    {
        // Trigger connection setup
        case NetworkState::NetworkIdle:
        {
            logger.log("[FSM] Idle -> starting connection");

            modem.establish_network_connection();

            logger.log("[FSM] -> CONNECTING");

            current_state = NetworkState::NetworkConnecting;
            break;
        }

        // wait until an IP address has been assigned
        case NetworkState::NetworkConnecting:
        {
            if (modem.is_network_ready())
            {
                
                logger.log("[FSM] Network READY");

                current_state = NetworkState::NetworkReady;
            }
            else
            {
                logger.log("[FSM] NO IP YET (SIM/Signal/APN/PDP) -> ERROR");

                current_state = NetworkState::NetworkError;
            }
            break;
        }
        
        // IP available, set up the MQTT client and connect to the broker
        case NetworkState::NetworkReady:
        {
            if (!modem.is_network_ready())
            {
                current_state = NetworkState::NetworkIdle;
                break;
            }

            modem.mqtt_disconnect();

            if (modem.mqtt_start() &&
                modem.mqtt_create_client() &&
                modem.mqtt_connect())
            {
                consecutive_publish_failures = 0;
                last_health_check = clock.get_clock_ticks();
                current_state = NetworkState::NetworkMqttConnected;
            }
            else
            {
                logger.log("[FSM] MQTT connection failed -> ERROR");
                current_state = NetworkState::NetworkError;
            }
            break;
        }

        case NetworkState::NetworkMqttConnected:
        {
            bool lost_connection = false;

            if (clock.get_clock_ticks() - last_health_check >= HEALTH_CHECK_INTERVAL_MS)
            {
                last_health_check = clock.get_clock_ticks();

                if (!modem.is_network_ready())
                    lost_connection = true;
            }

            if (consecutive_publish_failures >= MAX_CONSECUTIVE_PUBLISH_FAILURES)
                lost_connection = true;

            if (lost_connection)
            {
                logger.log("[FSM] Connection lost -> clean reinit");

                consecutive_publish_failures = 0;
                modem.mqtt_disconnect();

                current_state = NetworkState::NetworkError;
            }

            break;
        }

        // The error state is only a transit state, jumps straight back to Idle
        case NetworkState::NetworkError:
        {
            logger.log("[FSM] ERROR -> reset");

            current_state = NetworkState::NetworkIdle;
            break;
        }
    }
}
