#include "main.h"

#ifdef __cplusplus
#include "gnss_states.hpp"
#endif

#ifdef __cplusplus
extern "C" {
#endif

void gnss_fsm_step();

#ifdef __cplusplus
}

GnssState gnss_fsm_get_state();
const char* gnss_fsm_get_last_response();
bool gnss_fsm_has_valid_fix();
#endif
