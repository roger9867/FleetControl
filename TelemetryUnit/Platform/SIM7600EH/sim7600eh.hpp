#pragma once

#include "../Uart.hpp"

class Sim7600
{
    public:
        Sim7600(Uart* _huart_sim, Uart* _huart_debug);
        Sim7600() = default;

        bool lte_init();
        bool check_connection();
        bool check_sim();
        bool check_network();
        bool check_signal();
        bool check_attach();
        bool set_apn(const char* apn);
        bool activate_pdp();
        bool get_ip(char* out_ip, uint16_t max_len);

        bool activate_gnss();
        bool gnss_nmea_start();
        const char* get_gnss();

    private:
        Uart* huart_sim;
        Uart* huart_debug;

        void memset(uint8_t *buf, uint8_t value, uint32_t len);
};
