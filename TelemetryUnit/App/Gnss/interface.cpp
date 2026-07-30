#include "interface.h"
#include "gnss_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"

static Sim7600 sim;
static GnssFSM gnss_fsm(sim);


extern "C" void gnss_fsm_step()
{
    gnss_fsm.step();
}

GnssState gnss_fsm_get_state()
{
    return gnss_fsm.get_state();
}

const char* gnss_fsm_get_last_response()
{
    return gnss_fsm.get_last_response();
}

bool gnss_fsm_has_valid_fix()
{
    return gnss_fsm.has_valid_fix();
}
