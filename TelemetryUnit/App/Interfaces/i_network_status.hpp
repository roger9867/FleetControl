#pragma once

// What other FSMs need to know about the network connection
class INetworkStatus
{
    public:
        // True while the MQTT connection to the broker is established
        virtual bool is_mqtt_connected() const = 0;

        // Reports the result of a publish attempt, so repeated failures can be detected
        virtual void report_publish_result(bool ok) = 0;

    protected:
        ~INetworkStatus() = default;
};
