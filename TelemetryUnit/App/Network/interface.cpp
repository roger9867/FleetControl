#include "interface.h"
#include "network_fsm.hpp"
#include "gnss_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"

static Sim7600 sim;
static NetworkFSM network_fsm(sim);
static GnssFSM gnss_fsm(sim);

extern "C" void network_fsm_step()
{
    network_fsm.step();
}

extern "C" void gnss_fsm_step()
{
    gnss_fsm.step();
}