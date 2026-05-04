#ifndef __MPU_6050_H__
#define __MPU_6050_H__

#include "main.h"
#include "i2c.h"
#include "usart.h"
#include "../PrintUtils/print_utils.h"

/**
 * @brief Prints important MPU6050 configuration registers via UART for debugging.
 *
 * Reads some MPU6050 configuration registers over I2C and sends
 * their values over UART.
 *
 * Printed registers:
 * - WHO_AM_I
 * - PWR_MGMT_1
 * - SMPLRT_DIV
 * - CONFIG
 * - GYRO_CONFIG
 * - ACCEL_CONFIG
 * - FIFO_EN
 * - USER_CONTROL
 *
 * @param hi2cX Pointer to the I2C handle used for MPU6050 communication.
 * @param huart Pointer to the UART handle used for output.
 */
void print_config_registers(I2C_HandleTypeDef* hi2cX, UART_HandleTypeDef* huart);

#endif /* __MPU_6050_H__ */
