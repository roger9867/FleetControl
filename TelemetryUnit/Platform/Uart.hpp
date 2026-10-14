#pragma once

#include "stm32f1xx_hal.h"
#include <cstdint>
#include "usart.h"
#include "cmsis_os.h"

extern osMutexId_t huart_sim_mutexHandle;
extern osMutexId_t huart_debug_mutexHandle;

// Wraps UART usage and protects the lines with RTOS mutexes
class Uart
{
    public:
        /** @param _huart HAL handle */
        Uart(UART_HandleTypeDef* _huart, osMutexId_t& huart_sim_mutexHandle);

        // Read function for AT command responses of the SIM7600 module
        HAL_StatusTypeDef read(
            uint8_t* buffer,
            uint16_t max_len,
            uint32_t timeout_ms
        );

        // Read function for raw bytes for command reception (ID output) and PC debugging
        HAL_StatusTypeDef readRaw(
            uint8_t* buffer,
            uint16_t max_len,
            uint32_t timeout_ms
        );

        // Send len bytes
        HAL_StatusTypeDef write(
            uint8_t* data,
            uint16_t len,
            uint32_t timeout_ms = HAL_MAX_DELAY
        );


    private:
        UART_HandleTypeDef* huart;
        osMutexId_t& mutex;

        // Mutex functions
        void acquire();
        void release();
};
