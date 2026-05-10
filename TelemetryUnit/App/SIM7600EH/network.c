#include "network.h"


void sim7600_lte_init(UART_HandleTypeDef* huart_sim)
{
    uint8_t cmd[64];

    // Check connection to sim7600e-h
    uint8_t at_cmd[] = "AT\r\n";
    HAL_UART_Transmit(huart_sim, at_cmd, sizeof(at) - 1, HAL_MAX_DELAY);
    osDelay(500);

    uint8_t echo_disable_cmd[] = "ATE0\r\n";
    HAL_UART_Transmit(huart_sim, echo_disable_cmd, sizeof(echo_disable_cmd)-1, HAL_MAX_DELAY);
    osDelay(500);

    // Enter SIM pin
    //const char* pin = "1967";

    //int len = snprintf((char*)cmd, sizeof(cmd),
    //                  "AT+CPIN=\"%s\"\r\n", pin);

    HAL_UART_Transmit(huart_sim, cmd, len, HAL_MAX_DELAY);
    osDelay(2000);

    // SIM Status check
    uint8_t check_status_cmd[] = "AT+CPIN?\r\n";
    HAL_UART_Transmit(huart_sim, check_status_cmd, sizeof(check_status_cmd)-1, HAL_MAX_DELAY);
    osDelay(500);

    // Enable network mode
    uint8_t activate_network_cmd[] = "AT+CFUN=1\r\n";
    HAL_UART_Transmit(huart_sim, activate_network_cmd, sizeof(activate_network_cmd)-1, HAL_MAX_DELAY);
    osDelay(3000);

    // Set network provider APN token
    uint8_t apn[] = "AT+CGDCONT=1,\"IP\",\"internet\"\r\n";
    HAL_UART_Transmit(huart_sim, apn, sizeof(apn) - 1, HAL_MAX_DELAY);
    osDelay(500);

    // Check network state
    uint8_t reg[] = "AT+CEREG?\r\n";
    HAL_UART_Transmit(huart_sim, reg, sizeof(reg) - 1, HAL_MAX_DELAY);
    osDelay(500);

    // Enable PDP 
    uint8_t attach[] = "AT+CGATT=1\r\n";
    HAL_UART_Transmit(huart_sim, attach, sizeof(attach) - 1, HAL_MAX_DELAY);
    osDelay(2000);

    // IP check
    uint8_t ip[] = "AT+CGPADDR=1\r\n";
    HAL_UART_Transmit(huart_sim, ip, sizeof(ip) - 1, HAL_MAX_DELAY);
}









void sim7600_lte_status(UART_HandleTypeDef* huart_sim, UART_HandleTypeDef* huart_debug)
{
    uint8_t cmd[] = "AT+CSQ\r\n";
    uint8_t buf[100];
    uint8_t c;
    uint16_t i = 0;

    HAL_UART_Transmit(huart_sim, cmd, sizeof(cmd)-1, HAL_MAX_DELAY);

    for(i = 0; i < sizeof(buf)-1; i++)
    {
        if(HAL_UART_Receive(huart_sim, &c, 1, 500) != HAL_OK)
            break;

        buf[i] = c;
    }

    buf[i] = '\0';

    // OUTPUT to PC (USART2)
    HAL_UART_Transmit(huart_debug, buf, i, HAL_MAX_DELAY);

    uint8_t end[] = "\r\n[STATUS DONE]\r\n";
    HAL_UART_Transmit(huart_debug, end, sizeof(end)-1, HAL_MAX_DELAY);
}