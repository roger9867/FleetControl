#pragma once

#include "main.h"

#ifdef __cplusplus
#include "../Interfaces/i_network_status.hpp"
#endif

#ifdef __cplusplus
extern "C" {
#endif

void network_fsm_step();

#ifdef __cplusplus
}

// Network FSM as seen by other FSMs (connection status and publish feedback)
INetworkStatus& network_fsm_status();
#endif
