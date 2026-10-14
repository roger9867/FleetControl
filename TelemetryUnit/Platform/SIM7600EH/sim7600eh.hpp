#pragma once

#include "cmsis_os.h"

#include "../../App/Interfaces/i_mqtt_publisher.hpp"
#include "../../App/Interfaces/i_gnss_modem.hpp"
#include "../../App/Interfaces/i_network_modem.hpp"
#include "../Uart.hpp"
#include "firmware_secrets.h"


extern osMutexId_t huart_sim_mutexHandle;
extern osMutexId_t huart_debug_mutexHandle;


// Driver for the SIM7600EH module:
// Contains the direct communication with
// the Waveshare SIM7600E-H LTE/GNSS module
class Sim7600 : public IMqttPublisher, public IGnssModem, public INetworkModem
{
    public:
        Sim7600() {}

        // Checks whether a SIM card is inserted and unlocked (AT+CPIN?)
        bool check_sim();

        // Checks network registration with the mobile operator (AT+CREG?)
        bool check_network();

        //Checks the signal quality (AT+CSQ)
        bool check_signal();

        // Checks the attachment to the packet data service (AT+CGATT?)
        bool check_attach();

        // Sets the APN for the PDP context activation
        bool set_apn();

        // Activates the PDP context, i.e. the actual data connection
        bool activate_pdp();

        // Queries the currently assigned IP address (AT+CGPADDR)
        bool get_ip(char* out_ip, uint16_t max_len);

        // Executes all steps for the network setup one after another
        void establish_network_connection() override;

        // Returns true as soon as an IP address has been assigned
        bool is_network_ready() override;


        // Switches GNSS on
        bool activate_gnss() override;

        // Starts the NMEA position output
        bool gnss_nmea_start() override;
        void get_gnss(uint8_t* buffer, uint16_t buffer_length);
        bool has_fix();

        // Queries the current GNSS position (AT+CGNSSINFO) and returns
        // true if a valid fix is available
        bool get_gnss_fix(uint8_t* buffer, uint16_t buffer_length) override;


        // Starts the module's MQTT client (AT+CMQTTSTART)
        bool mqtt_start() override;

        // Creates an MQTT client instance (AT+CMQTTACCQ)
        bool mqtt_create_client() override;

        // Establishes the connection to the configured broker
        bool mqtt_connect() override;

        // Cleanly disconnects from the broker
        bool mqtt_disconnect() override;

        // Publishes payload as a JSON data point on the configured topic
        bool mqtt_publish(const char* payload);

        bool is_mqtt_connected();

        // Stops the module's MQTT client
        bool mqtt_stop();

        // IMqttPublisher: forwards to mqtt_publish()
        bool publish(const char* payload) override;


        Uart huart_sim = Uart(&huart3, huart_sim_mutexHandle);
        Uart huart_debug = Uart(&huart2, huart_debug_mutexHandle);

    private:
        // MQTT Credentials
        const char* IP_BROKER = FW_BROKER_HOST;
        uint32_t PORT_BROKER = FW_BROKER_PORT;
        const char* MQTT_USER = FW_MQTT_USER;
        const char* MQTT_PASSWORD = FW_MQTT_PASSWORD;
        const char* TOPIC_NAME = FW_MQTT_TOPIC;
        const char* APN_TOKEN = FW_APN;

        // Helper function
        void memset(uint8_t *buf, uint8_t value, uint32_t len);
};
