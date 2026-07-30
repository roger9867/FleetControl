#include "main.h"

#ifdef __cplusplus
#include "network_states.hpp"
#endif

#ifdef __cplusplus
extern "C" {
#endif

void network_fsm_step();
void network_fsm_report_publish_result(bool ok);

#ifdef __cplusplus
}

NetworkState network_fsm_get_state();
#endif
