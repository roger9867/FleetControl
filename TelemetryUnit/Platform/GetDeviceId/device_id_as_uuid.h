#ifndef __DEVICE_ID_AS_UUID_H__
#define __DEVICE_ID_AS_UUID_H__

#include "stm32f1xx_hal.h"
#include "../PrintUtils/print_utils.h"

#ifdef __cplusplus
extern "C" {
#endif

// Erzeugt einen UUID-formatierten String aus der eindeutigen STM32-Geraete-ID.
// Liest die 96-Bit STM32-Hardware-UID und wandelt sie in einen UUID-String
// im Format XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX um.
// out muss auf einen Puffer von mindestens 37 Byte zeigen
// (36 Zeichen + Nullterminierung).
void stm32_get_uid_as_uuid(char *out);

#ifdef __cplusplus
}
#endif

#endif  // __DEVICE_ID_AS_UUID_H__
