#include "interface.h"
#include "network_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"

static Sim7600 sim;
static NetworkFSM network_fsm(sim);

extern "C" void network_fsm_step()
{
    network_fsm.step();
}
