#pragma once

#include "../Uart.hpp"
#include "at_buffer.hpp"
#include "cmsis_os.h"

extern osMutexId_t huart_sim_mutexHandle;
extern osMutexId_t huart_debug_mutexHandle;

class Sim7600
{
    public:
        Sim7600() {}

        bool check_connection();
        bool check_sim();
        bool check_network();
        bool check_signal();
        bool check_attach();
        bool set_apn();
        bool activate_pdp();
        bool get_ip(char* out_ip, uint16_t max_len);

        void establish_network_connection();
        bool is_network_ready();



        bool activate_gnss();
        bool gnss_nmea_start();
        void get_gnss(uint8_t* buffer, uint16_t buffer_length);
        bool has_fix();

        Uart huart_sim = Uart(&huart3, huart_sim_mutexHandle);
        Uart huart_debug = Uart(&huart2, huart_debug_mutexHandle);
        AtBuffer at_buffer;

        void on_uart_rx(uint8_t c);
        bool wait_for(const char* token, uint32_t timeout);
        void send_cmd(const char* cmd);


        bool mqtt_start();
        bool mqtt_create_client();
        bool mqtt_connect();
        bool mqtt_disconnect();
        bool mqtt_publish(const char* payload);
        bool is_mqtt_connected();
        bool mqtt_stop();


    private:
        const char* APN_TOKEN = "internet";
        const char* TOPIC_NAME = "drivedata";
        const char* IP_BROKER = "74.248.185.45";
        const char* MQTT_USER = "mqttuser";
        const char* MQTT_PASSWORD = "hihohihe1967";
        uint32_t PORT_BROKER = 1883;

        void memset(uint8_t *buf, uint8_t value, uint32_t len);
        bool wait_for_char(char token, uint32_t timeout);



};
