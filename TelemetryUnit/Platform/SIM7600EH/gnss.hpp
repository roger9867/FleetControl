#pragma once

#include "main.h"
#include "usart.h"
#include "../PrintUtils/print_utils.h"
#include "FreeRTOS.h"
#include "task.h"
#include "cmsis_os.h"
#include <stdio.h>

extern osMutexId_t huart_sim_mutexHandle;
extern osMutexId_t huart_debug_mutexHandle;


class Gnss
{
    public:
        void confirm_connection();

        void sim7600_gnss_on();
        void sim7600_get_gps();
        void sim7600_gnss_nmea_start();

        Uart huart_sim = Uart(&huart3, huart_sim_mutexHandle);
        Uart huart_debug = Uart(&huart2, huart_debug_mutexHandle);

    private:

};
