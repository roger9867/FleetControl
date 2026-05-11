#include "mpu6050.h"

#define MPU6050_ADDR 0x68 << 1  // um 1 shiften: I2c-Adresse 7 bit linksbündig +r/w-Bit

#define WHO_AM_I 0x75 // device identification 
#define PWR_MGMT_1 0x6B // Power management, CLK source (0 = internal oscillator)
#define MPU6050_SMRT_DIV 0x19
#define MPU6050_CONFIG 0x1A // DLPF config
#define MPU6050_GYRO_CONFIG 0x1B // Full range
#define MPU6050_ACCEL_CONFIG 0x1C // Accel. full scale range = Messbereich
#define MPU6050_FIFO_EN 0x23 //FIFO enable / disable
#define MPU6050_USER_CONTROL 0x6A // enable FIFO buffers b6=1, b2=FIFO reset=1 wenn FIFO_En=0 -> clears to 0 after

#define MPU6050_GYRO_OUT_H 0x43
#define MPU6050_ACCEL_OUT_H 0x3B
#define MPU6050_TEMP_OUT_H 0x41

#define MPU6050_SAMPLE_RATE_AFTER_DIV 100 // 100 Hz
#define MPU6059_SMRT_DIV 9  // SMRT_DIV = -1 + 1kHz(DLPF=1) / 100Hz



void print_register(
    I2C_HandleTypeDef* hi2cX,
    UART_HandleTypeDef* huart,
    const char* name,
    uint8_t reg
) {
    uint8_t data;

    HAL_I2C_Mem_Read(
        hi2cX,
        MPU6050_ADDR,
        reg,
        I2C_MEMADD_SIZE_8BIT,
        &data,
        1,
        HAL_MAX_DELAY
    );

    char out[9] = "";
    u8toStringBin(out, data);

    HAL_UART_Transmit(huart, (uint8_t*)out, 8, HAL_MAX_DELAY);
    uprints(huart, "    ");
    uprints(huart, name);
    
    uprints(huart, "\r\n");
}


void print_config_registers(
    I2C_HandleTypeDef* hi2cX,
    UART_HandleTypeDef* huart
) {
    print_register(hi2cX, huart, "WHO_AM_I",         WHO_AM_I);
    print_register(hi2cX, huart, "PWR_MGMT_1",       PWR_MGMT_1);
    print_register(hi2cX, huart, "SMPLRT_DIV",       MPU6050_SMRT_DIV);
    print_register(hi2cX, huart, "CONFIG",           MPU6050_CONFIG);
    print_register(hi2cX, huart, "GYRO_CONFIG",      MPU6050_GYRO_CONFIG);
    print_register(hi2cX, huart, "ACCEL_CONFIG",     MPU6050_ACCEL_CONFIG);
    print_register(hi2cX, huart, "FIFO_EN",          MPU6050_FIFO_EN);
    print_register(hi2cX, huart, "USER_CONTROL",     MPU6050_USER_CONTROL);

    uprints(huart, "\r\n");
}
