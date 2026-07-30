#include "interface.h"
#include "network_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"

static Sim7600 sim;
static NetworkFSM network_fsm(sim);

extern "C" void network_fsm_step()
{
    network_fsm.step();
}

extern "C" void network_fsm_report_publish_result(bool ok)
{
    network_fsm.report_publish_result(ok);
}

NetworkState network_fsm_get_state()
{
    return network_fsm.get_state();
}
