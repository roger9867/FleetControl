#include "Uart.hpp"

#include <cstring>

Uart::Uart(UART_HandleTypeDef* _huart, osMutexId_t& huart_sim_mutexHandle)
    : huart(_huart), mutex(huart_sim_mutexHandle)
{
}

HAL_StatusTypeDef Uart::read(
    uint8_t* buffer,
    uint16_t max_len,
    uint32_t timeout_ms
) {
    uint16_t idx = 0;
    uint8_t c;

    // Get the current time in ticks of the SysTick timer register
    uint32_t startTime = HAL_GetTick();

    acquire();

    while ((HAL_GetTick() - startTime) < timeout_ms && idx < max_len)
    {
        if (HAL_UART_Receive(huart, &c, 1, 10) == HAL_OK)
        {
            buffer[idx++] = c;
            buffer[idx] = '\0';

            // Reset the timeout as long as data keeps arriving
            startTime = HAL_GetTick();

            // Prompt for sending topic/payload
            if (strchr((char*)buffer, '>') != nullptr)
                break;

            // Normal AT response finished
            if (strstr((char*)buffer, "\r\nOK\r\n") != nullptr)
                break;

            // Error also finishes it
            if (strstr((char*)buffer, "\r\nERROR\r\n") != nullptr)
                break;
        }
    }

    release();

    buffer[idx] = '\0';

    return (idx > 0) ? HAL_OK : HAL_TIMEOUT;
}

HAL_StatusTypeDef Uart::readRaw(
    uint8_t* buffer,
    uint16_t max_len,
    uint32_t timeout_ms
) {
    uint16_t idx = 0;
    uint8_t c;

    // Get the current time in ticks of the SysTick timer register
    uint32_t startTime = HAL_GetTick();

    acquire();

    while ((HAL_GetTick() - startTime) < timeout_ms && idx < max_len)
    {
        if (HAL_UART_Receive(huart, &c, 1, 10) == HAL_OK)
        {
            buffer[idx++] = c;
        }
    }

    release();

    return (idx > 0) ? HAL_OK : HAL_TIMEOUT;
}

HAL_StatusTypeDef Uart::write(
    uint8_t* data,
    uint16_t len,
    uint32_t timeout_ms
) {
    acquire();

    HAL_StatusTypeDef status =
        HAL_UART_Transmit(huart, data, len, timeout_ms);

    release();

    return status;
}

void Uart::acquire()
{
    osMutexAcquire(mutex, osWaitForever);
}

void Uart::release()
{
    osMutexRelease(mutex);
}
