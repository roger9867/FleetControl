#ifndef __DEVICE_ID_AS_UUID_H__
#define __DEVICE_ID_AS_UUID_H__

#include "stm32f1xx_hal.h"
#include "../PrintUtils/print_utils.h"

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Generates a UUID-formatted string from the STM32 unique device ID.
 *
 * Reads the 96-bit STM32 hardware unique identifier and converts it into
 * a UUID-style string using the format:
 *  XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
 *
 * @param[out] out Pointer to a character buffer that receives UUID string.
 *                 The buffer must be at least 37 bytes long
 *                 (36 characters + null terminator).
 */
void stm32_get_uid_as_uuid(char *out);

#ifdef __cplusplus
}
#endif

#endif  // __DEVICE_ID_AS_UUID_H__
