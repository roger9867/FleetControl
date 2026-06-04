#include "Uart.hpp"

Uart::Uart(UART_HandleTypeDef* _huart, osMutexId_t& huart_sim_mutexHandle)
    : huart(_huart), mutex(huart_sim_mutexHandle)
{
    //init();
}

HAL_StatusTypeDef Uart::read(
    uint8_t* buffer,
    uint16_t max_len,
    uint32_t timeout_ms)
{
    uint16_t idx = 0;
    uint8_t c;

    uint32_t start = HAL_GetTick();

    acquire();

    while ((HAL_GetTick() - start) < timeout_ms && idx < (max_len - 1))
    {
        if (HAL_UART_Receive(huart, &c, 1, 10) == HAL_OK)
        {
            buffer[idx++] = c;
            start = HAL_GetTick();
        }
    }

    release();

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

    acquire();
    while ((HAL_GetTick() - start) < timeout_ms && idx < max_len)
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
    uint32_t timeout_ms)
{
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

/*
void Uart::init()
{
    osMutexAttr_t attr = {
        .name = "uart_mutex"
    };

    mutex = osMutexNew(&attr);
}
*/