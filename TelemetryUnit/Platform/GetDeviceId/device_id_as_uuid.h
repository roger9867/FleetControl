#ifndef __DEVICE_ID_AS_UUID_H__
#define __DEVICE_ID_AS_UUID_H__

#include "stm32f1xx_hal.h"
#include "../PrintUtils/print_utils.h"

#ifdef __cplusplus
extern "C" {
#endif

// Creates a UUID-formatted string from the unique STM32 device ID.
// Reads the 96-bit STM32 hardware UID and converts it into a UUID string
// in the format XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX.
// out must point to a buffer of at least 37 bytes
// (36 characters + null terminator).
void stm32_get_uid_as_uuid(char *out);

#ifdef __cplusplus
}
#endif

#endif  // __DEVICE_ID_AS_UUID_H__
