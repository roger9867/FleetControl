#pragma once

#include <cstdint>


class IClock
{
    public:
        virtual uint32_t get_clock_ticks() = 0;

    protected:
        ~IClock() = default;
};
