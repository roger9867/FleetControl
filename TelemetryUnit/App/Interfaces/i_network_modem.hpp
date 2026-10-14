#pragma once

// Cellular and MQTT connection functions of the modem, as needed by NetworkFSM
class INetworkModem
{
    public:
        // Executes all steps for the network setup one after another
        virtual void establish_network_connection() = 0;

        // Returns true as soon as an IP address has been assigned
        virtual bool is_network_ready() = 0;

        // Starts the module's MQTT client
        virtual bool mqtt_start() = 0;

        // Creates an MQTT client instance
        virtual bool mqtt_create_client() = 0;

        // Establishes the connection to the configured broker
        virtual bool mqtt_connect() = 0;

        // Cleanly disconnects from the broker
        virtual bool mqtt_disconnect() = 0;

    protected:
        ~INetworkModem() = default;
};
