#ifndef __GET_DEVICE_ID_H__
#define __GET_DEVICE_ID_H__

#include "device_id_as_uuid.h"
#include "cmsis_os.h"

extern osMutexId_t huart_debug_mutexHandle;

#ifdef __cplusplus
extern "C" {
#endif

// Prüft eingehende UART-Befehle und fuehrt sie aus.
// Liest Bytes vom Debug-UART, baut daraus einen Befehlsstring auf
// und führt passende Befehle aus, sobald ein Zeilenende empfangen wurde.
//
// Unterstuetzter Befehl:
// - get_device_id, liefert die Device-ID als UUID formatiert
//
// Diese Funktion muss wiederholt aufgerufen werden.
void check_for_device_id_request_command(void);

#ifdef __cplusplus
}
#endif

#endif  //__GET_DEVICE_ID_H__
