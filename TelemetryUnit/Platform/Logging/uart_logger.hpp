#pragma once

#include "../../App/Interfaces/i_logger.hpp"
#include "../Uart.hpp"
#include <cstring>


class UartLogger : public ILogger
{
    public:
        explicit UartLogger(Uart& _uart) : uart(_uart) {}

        void log(const char* line) override;

    private:
        Uart& uart;
};
