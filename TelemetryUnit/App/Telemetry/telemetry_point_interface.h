#pragma once

#include "main.h"

#ifdef __cplusplus
extern "C" {
#endif

bool activate_gnss();
bool gnss_nmea_start();
const char* get_gnss();

#ifdef __cplusplus
}
#endif

