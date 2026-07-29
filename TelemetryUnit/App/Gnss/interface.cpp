#include "interface.h"
#include "gnss_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"

static Sim7600 sim;
static GnssFSM gnss_fsm(sim);


extern "C" void gnss_fsm_step()
{
    gnss_fsm.step();
}
