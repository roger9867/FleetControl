#include "telemetry_point_interface.h"


static Uart uart_sim(&huart3);
static Uart uart_debug(&huart2);

static Sim7600 sim(&uart_sim, &uart_debug);

extern "C" bool lte_init()
{
    sim.lte_init();
}