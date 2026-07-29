#include "network_fsm.hpp"
#include <string.h>

#include <cstdio>

NetworkFSM::NetworkFSM(Sim7600& _network_module)
: network_module(_network_module)
{
}

void NetworkFSM::step()
{
    switch (current_state)
    {
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
                const char msg[] = "[FSM] MQTT CONNECT FAILED\r\n";
                network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                current_state = NetworkState::NetworkError;
            }
            break;
        }
        
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
                current_state = NetworkState::NetworkMqttConnected;
            }

            break;
        }

        case NetworkState::NetworkMqttConnected:
        {
            static uint32_t last_publish = 0;

            if (HAL_GetTick() - last_publish >= 1000)
            {
                last_publish = HAL_GetTick();

                uint32_t t = HAL_GetTick();

                const char msg[] = "[FSM] MQTT SEND\r\n";
                network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                network_module.mqtt_publish("222.2222");

                char buf[64];
                sprintf(buf, "[FSM] Publish time: %lu ms\r\n", HAL_GetTick() - t);
                network_module.huart_debug.write((uint8_t*)buf, strlen(buf));
            }
            break;
        }

        case NetworkState::NetworkError:
        {
            const char msg[] = "[FSM] ERROR -> reset\r\n";
            network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

            current_state = NetworkState::NetworkIdle;
            break;
        }
    }
}
