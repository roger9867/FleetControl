#include "hal_clock.hpp"
#include "stm32f1xx_hal.h"


uint32_t HalClock::get_clock_ticks()
{
    return HAL_GetTick();
}
