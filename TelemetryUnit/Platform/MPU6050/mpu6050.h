#ifndef __MPU_6050_H__
#define __MPU_6050_H__

#include "main.h"
#include "i2c.h"
#include "usart.h"
#include "../PrintUtils/print_utils.h"

// Prints important MPU6050 configuration registers via UART for debugging
void print_config_registers(I2C_HandleTypeDef* hi2cX, UART_HandleTypeDef* huart);

#endif /* __MPU_6050_H__ */
