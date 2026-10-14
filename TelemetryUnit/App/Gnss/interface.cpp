#include "interface.h"
#include "gnss_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "../../Platform/Clock/hal_clock.hpp"
#include "../../Platform/Logging/uart_logger.hpp"

// Platform implementations of the interfaces used by the GNSS FSM
static Sim7600 sim;
static HalClock clock;
static UartLogger logger(sim.huart_debug);

static GnssFSM gnss_fsm(sim, clock, logger);


extern "C" void gnss_fsm_step()
{
    gnss_fsm.step();
}

IGnssSource& gnss_fsm_source()
{
    return gnss_fsm;
}
