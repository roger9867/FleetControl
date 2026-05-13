#pragma once

#include "main.h"

#ifdef __cplusplus
extern "C" {
#endif

bool check_connection();
bool sim_check();
bool check_network();
bool check_signal();
bool check_attach();
bool set_apn();
bool activate_pdp();
bool get_ip(char* out_ip, uint16_t max_len);

bool activate_gnss();
bool gnss_nmea_start();
const char* get_gnss();

#ifdef __cplusplus
}
#endif