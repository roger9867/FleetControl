#ifndef __PRINT_UTILS_H__
#define __PRINT_UTILS_H__

#include "main.h"

/**
 * @brief Sends a null-terminated string over UART.
 * @param huart UART handle used for transmission.
 * @param message Pointer to null-terminated string to send.
 */
void uprints(UART_HandleTypeDef* huart, const char* message);

/**
 * @brief Converts a uint32_t value to a decimal string.
 * @param inbuf Output buffer for the resulting string.
 * @param uint Unsigned 32-bit integer to convert.
 */
void u32toStringDec(char* inbuf, uint32_t uint);

/**
 * @brief Converts an int32_t value to a decimal string.
 * @param inbuf Output buffer for the resulting string.
 * @param _int Signed 32-bit integer to convert.
 */
void i32toStringDec(char* inbuf, int32_t _int);

/**
 * @brief Converts an int32_t value to a hexadecimal string.
 * @param out Output buffer for the hex string.
 * @param _int Integer value to convert.
 */
void i32toStringHex(char* out, int32_t _int);

/**
 * @brief Converts an int32_t value to a binary string.
 * @param out Output buffer for the binary string.
 * @param _int Integer value to convert.
 */
void i32toStringBin(char* out, int32_t _int);

/**
 * @brief Converts memory content at a given address to string.
 * @param out Output buffer for the string representation.
 * @param address Pointer to memory location.
 */
void addressContentToString(char* out, void* address);


/**
 * @brief Converts a float value to a decimal string.
 * @param buf Output buffer for the resulting string.
 * @param val Floating-point value to convert.
 */
void f32toStringDec(char* buf, float val);

/**
 * @brief Converts an 8-bit unsigned value to binary string.
 * @param out Output buffer for binary representation.
 * @param val 8-bit unsigned value.
 */
void u8toStringBin(char* out, uint8_t val);

#endif  // __PRINT_UTILS_H__
