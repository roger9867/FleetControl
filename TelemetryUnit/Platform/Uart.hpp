#pragma once

#include "stm32f1xx_hal.h"
#include <cstdint>
#include "usart.h"
#include "cmsis_os.h"

extern osMutexId_t huart_sim_mutexHandle;
extern osMutexId_t huart_debug_mutexHandle;

class Uart
{
public:
    Uart(UART_HandleTypeDef* _huart, osMutexId_t& huart_sim_mutexHandle);

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
    UART_HandleTypeDef* huart;
    osMutexId_t& mutex;

    void acquire();
    void release();
    //void init();
};
