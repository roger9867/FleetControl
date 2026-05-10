#ifndef __SIM7600EH_GNSS_H__
#define __SIM7600EH_GNSS_H__

#include "main.h"
#include "usart.h"
#include "../PrintUtils/print_utils.h"
#include "FreeRTOS.h"
#include "task.h"
#include "cmsis_os.h"
#include <stdio.h>

void confirm_connection(UART_HandleTypeDef* huart_to_pc, UART_HandleTypeDef* huart_to_sim7600);

void sim7600_gnss_on(UART_HandleTypeDef* sim);
void sim7600_get_gps(UART_HandleTypeDef* pc, UART_HandleTypeDef* sim);
void sim7600_gnss_nmea_start(UART_HandleTypeDef* sim);

#endif  /* __SIM7600EH_GNSS_H__ */
