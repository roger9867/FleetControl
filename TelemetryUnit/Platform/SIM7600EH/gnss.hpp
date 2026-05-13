#pragma once

#include "main.h"
#include "usart.h"
#include "../PrintUtils/print_utils.h"
#include "FreeRTOS.h"
#include "task.h"
#include "cmsis_os.h"
#include <stdio.h>


class Gnss
{
    public:
        void confirm_connection();

        void sim7600_gnss_on();
        void sim7600_get_gps();
        void sim7600_gnss_nmea_start();

        Uart huart_sim = Uart(&huart3);
        Uart huart_debug = Uart(&huart2);

    private:

};


    