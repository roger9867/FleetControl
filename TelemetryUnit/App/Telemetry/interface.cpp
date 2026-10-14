#include "interface.h"
#include "telemetry_fsm.hpp"
#include "telemetry_point.hpp"
#include "../Network/interface.h"
#include "../Gnss/interface.h"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "../../Platform/Clock/hal_clock.hpp"
#include "../../Platform/Logging/uart_logger.hpp"
#include "../../Platform/GetDeviceId/stm32_device_id.hpp"

// Platform implementations of the interfaces used by the telemetry FSM
static Sim7600 sim;
static HalClock clock;
static UartLogger logger(sim.huart_debug);
static Stm32DeviceId device_id;

static TelemetryPoint telemetry_point(clock, device_id);
static TelemetryFsm telemetry_fsm(sim,
                                  network_fsm_status(),
                                  gnss_fsm_source(),
                                  telemetry_point,
                                  clock,
                                  logger);

extern "C" void telemetry_fsm_step()
{
    telemetry_fsm.step();
}
