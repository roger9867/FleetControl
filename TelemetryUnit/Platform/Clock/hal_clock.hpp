#pragma once


#include "stm32f1xx_hal.h"

#include "../../App/Interfaces/i_clock.hpp"


class HalClock : public IClock
{
    public:
        uint32_t get_clock_ticks() override;
};
