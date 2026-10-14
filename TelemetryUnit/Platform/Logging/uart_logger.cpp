#include "uart_logger.hpp"

void UartLogger::log(const char* line)
{
    uart.write((uint8_t*)line, strlen(line));
    uart.write((uint8_t*)"\r\n", 2);
}
