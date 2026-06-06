#include "network_fsm.hpp"
#include <string.h>

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
            break;
        }

        case NetworkState::NetworkReady:
        {
            if (!network_module.is_network_ready())
            {
                const char msg[] = "[FSM] LOST network -> IDLE\r\n";
                network_module.huart_debug.write((uint8_t*)msg, strlen(msg));

                current_state = NetworkState::NetworkIdle;
            }
            else 
            {
                const char msg[] = "[FSM] Network READY\r\n";
                network_module.huart_debug.write((uint8_t*)msg, strlen(msg));
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