#include "interface.h"
#include "telemetry_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"

static Sim7600 sim;
static TelemetryFsm telemetry_fsm(sim);

extern "C" void telemetry_fsm_step()
{
    telemetry_fsm.step();
}
