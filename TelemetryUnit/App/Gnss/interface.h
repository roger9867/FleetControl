#pragma once

#include "main.h"

#ifdef __cplusplus
#include "../Interfaces/i_gnss_source.hpp"
#endif

#ifdef __cplusplus
extern "C" {
#endif

void gnss_fsm_step();

#ifdef __cplusplus
}

// GNSS FSM as seen by other FSMs (readiness, fix and latest raw position data)
IGnssSource& gnss_fsm_source();
#endif
