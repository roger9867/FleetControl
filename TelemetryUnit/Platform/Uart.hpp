#pragma once

#include "stm32f1xx_hal.h"
#include <cstdint>

class Uart
{
public:
    explicit Uart(UART_HandleTypeDef* huart);

    HAL_StatusTypeDef read(
        uint8_t* buffer,
        uint16_t max_len,
        uint32_t timeout_ms
    );

    HAL_StatusTypeDef readRaw(
        uint8_t* buffer,
        uint16_t max_len,
        uint32_t timeout_ms                        
    );

    HAL_StatusTypeDef write(
        uint8_t* data,
        uint16_t len,
        uint32_t timeout_ms = HAL_MAX_DELAY
    );


private:
    UART_HandleTypeDef* _huart;
};
