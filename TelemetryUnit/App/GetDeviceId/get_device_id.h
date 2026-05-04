#ifndef __GET_DEVICE_ID_H__
#define __GET_DEVICE_ID_H__

#include "device_id_as_uuid.h"

/**
 * @brief Checks UART input for incoming commands and processes them.
 *
 * Reads bytes from the specified UART interface, builds a command string,
 * and executes matching commands when a line ending is received.
 *
 * Supported command:
 * - get_device_id formatted as UUID
 *
 * Responses are transmitted over the same UART interface.
 *
 * This function is to be called repeatedly.
 *
 * @param[in] huartx Pointer to the UART handle used for communication.
 */
void check_for_device_id_request_command(UART_HandleTypeDef* huartx);

#endif  //__GET_DEVICE_ID_H__
