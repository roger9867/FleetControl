#include "Uart.hpp"

Uart::Uart(UART_HandleTypeDef* huart)
    : _huart(huart)
{
}

HAL_StatusTypeDef Uart::read(
    uint8_t* buffer,
    uint16_t max_len,
    uint32_t timeout_ms)
{
    uint16_t idx = 0;
    uint8_t c;

    uint32_t start = HAL_GetTick();

    while ((HAL_GetTick() - start) < timeout_ms &&
           idx < (max_len - 1))
    {
        if (HAL_UART_Receive(_huart, &c, 1, 10) == HAL_OK)
        {
            buffer[idx++] = c;

            start = HAL_GetTick();
        }
    }

    buffer[idx] = '\0';

    return (idx > 0) ? HAL_OK : HAL_TIMEOUT;
}

HAL_StatusTypeDef Uart::readRaw(
    uint8_t* buffer,
    uint16_t max_len,
    uint32_t timeout_ms)
{
    uint16_t idx = 0;
    uint8_t c;

    uint32_t start = HAL_GetTick();

    while ((HAL_GetTick() - start) < timeout_ms && idx < max_len)
    {
        if (HAL_UART_Receive(_huart, &c, 1, 10) == HAL_OK)
        {
            buffer[idx++] = c;
        }
    }

    return (idx > 0) ? HAL_OK : HAL_TIMEOUT;
}

HAL_StatusTypeDef Uart::write(
    uint8_t* data,
    uint16_t len,
    uint32_t timeout_ms)
{
    return HAL_UART_Transmit(_huart, data, len, timeout_ms);
}
