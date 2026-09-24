#pragma once

#include "stm32f1xx_hal.h"
#include <cstdint>
#include "usart.h"
#include "cmsis_os.h"

extern osMutexId_t huart_sim_mutexHandle;
extern osMutexId_t huart_debug_mutexHandle;

// Kapselt UART-Nutzung und schützt die Leitungen mit RTOS-Mutexen
class Uart
{
    public:
        /** @param _huart HAL-Handle */
        Uart(UART_HandleTypeDef* _huart, osMutexId_t& huart_sim_mutexHandle);

        // Lesefunktion für AT-Kommando-Antworten des SIM7600-Moduls
        HAL_StatusTypeDef read(
            uint8_t* buffer,
            uint16_t max_len,
            uint32_t timeout_ms
        );

        // Lesefunktion für rohe Bytes für Befehlsempfang (ID-Ausgabe) und PC-Debugging
        HAL_StatusTypeDef readRaw(
            uint8_t* buffer,
            uint16_t max_len,
            uint32_t timeout_ms
        );

        // len Bytes Senden
        HAL_StatusTypeDef write(
            uint8_t* data,
            uint16_t len,
            uint32_t timeout_ms = HAL_MAX_DELAY
        );


    private:
        UART_HandleTypeDef* huart;
        osMutexId_t& mutex;

        // Mutex-Funktionen
        void acquire();
        void release();
};
