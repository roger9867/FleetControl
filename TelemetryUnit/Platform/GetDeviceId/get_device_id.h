#ifndef __GET_DEVICE_ID_H__
#define __GET_DEVICE_ID_H__

#include "device_id_as_uuid.h"
#include "cmsis_os.h"

extern osMutexId_t huart_debug_mutexHandle;

#ifdef __cplusplus
extern "C" {
#endif

// Checks incoming UART commands and executes them.
// Reads bytes from the debug UART, builds a command string from them
// and executes matching commands as soon as an end of line is received.
//
// Supported command:
// - get_device_id, returns the device ID formatted as a UUID
//
// This function must be called repeatedly.
void check_for_device_id_request_command(void);

#ifdef __cplusplus
}
#endif

#endif  //__GET_DEVICE_ID_H__
