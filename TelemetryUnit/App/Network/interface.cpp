#include "interface.h"
#include "network_fsm.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "../../Platform/Clock/hal_clock.hpp"
#include "../../Platform/Logging/uart_logger.hpp"

// Platform implementations of the interfaces used by the network FSM
static Sim7600 sim;
static HalClock clock;
static UartLogger logger(sim.huart_debug);

static NetworkFSM network_fsm(sim, clock, logger);

extern "C" void network_fsm_step()
{
    network_fsm.step();
}

INetworkStatus& network_fsm_status()
{
    return network_fsm;
}
