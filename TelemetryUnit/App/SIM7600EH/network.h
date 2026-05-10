#ifndef __NETWORK_H__
#define __NETWORK_H__

void sim7600_lte_init(UART_HandleTypeDef* sim);
//void SIM_NetworkTest(UART_HandleTypeDef *huart_modem, UART_HandleTypeDef *huart_debug);
void sim7600_lte_status(UART_HandleTypeDef* sim, UART_HandleTypeDef* pc);

/*
void sim7600_gnss_on(UART_HandleTypeDef* sim);
void sim7600_gnss_status(UART_HandleTypeDef* pc, UART_HandleTypeDef* sim);

void sim7600_gnss_stop(UART_HandleTypeDef* sim);*/

#endif  /*__NETWORK_H__*/
