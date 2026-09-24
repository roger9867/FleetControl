#include "network_fsm.hpp"
#include <string.h>

#include <cstdio>

namespace
{
    constexpr uint32_t HEALTH_CHECK_INTERVAL_MS = 5000;
    constexpr uint8_t MAX_CONSECUTIVE_PUBLISH_FAILURES = 3;
}

NetworkFSM::NetworkFSM(Sim7600& _network_module)
: network_module(_network_module)
{
}

NetworkState NetworkFSM::get_state()
{
    return current_state;
}

void NetworkFSM::report_publish_result(bool ok)
{
    consecutive_publish_failures = ok ? 0 : (consecutive_publish_failures + 1);
}

void NetworkFSM::step()
{
    switch (current_state)
    {
        // Verbindungsaufbau anstossen
        case NetworkState::NetworkIdle:
        {
            const char msg[] = "[FSM] Idle -> starting connection\r\n";
            network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

            network_module.establish_network_connection();

            const char msg2[] = "[FSM] -> CONNECTING\r\n";
            network_module.huart_debug.write((uint8_t*)msg2, strlen(msg2));

            current_state = NetworkState::NetworkConnecting;
            break;
        }

        // warten, bis eine IP-Adresse zugewiesen wurde
        case NetworkState::NetworkConnecting:
        {
            if (network_module.is_network_ready())
            {
                
                const char msg[] = "[FSM] Network READY\r\n";
                network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                current_state = NetworkState::NetworkReady;
            }
            else
            {
                const char msg[] = "[FSM] NO IP YET (SIM/Signal/APN/PDP) -> ERROR\r\n";
                network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                current_state = NetworkState::NetworkError;
            }
            break;
        }
        
        // IP vorhanden, MQTT-Client aufbauen und mit dem Broker verbinden
        case NetworkState::NetworkReady:
        {
            if (!network_module.is_network_ready())
            {
                current_state = NetworkState::NetworkIdle;
                break;
            }

            network_module.mqtt_disconnect();

            if (network_module.mqtt_start() &&
                network_module.mqtt_create_client() &&
                network_module.mqtt_connect())
            {
                consecutive_publish_failures = 0;
                last_health_check = HAL_GetTick();
                current_state = NetworkState::NetworkMqttConnected;
            }
            else
            {
                const char msg[] = "[FSM] MQTT connection failed -> ERROR\r\n";
                network_module.huart_debug.write(
                    (uint8_t*)msg, strlen(msg));
                    current_state = NetworkState::NetworkError;
            }
            break;
        }

        case NetworkState::NetworkMqttConnected:
        {
            bool lost_connection = false;

            if (HAL_GetTick() - last_health_check >= HEALTH_CHECK_INTERVAL_MS)
            {
                last_health_check = HAL_GetTick();

                if (!network_module.is_network_ready())
                    lost_connection = true;
            }

            if (consecutive_publish_failures >= MAX_CONSECUTIVE_PUBLISH_FAILURES)
                lost_connection = true;

            if (lost_connection)
            {
                const char msg[] = "[FSM] Connection lost -> clean reinit\r\n";
                network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                consecutive_publish_failures = 0;
                network_module.mqtt_disconnect();

                current_state = NetworkState::NetworkError;
            }

            break;
        }

        // Fehlerzustand ist nur ein Durchgangspunkt, springt sofort zurück zu Idle
        case NetworkState::NetworkError:
        {
            const char msg[] = "[FSM] ERROR -> reset\r\n";
            network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

            current_state = NetworkState::NetworkIdle;
            break;
        }
    }
}
