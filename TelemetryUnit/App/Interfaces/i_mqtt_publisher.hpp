#pragma once

// Abstraction of an MQTT connection that can publish a payload on the configured topic
class IMqttPublisher
{
    public:
    // Publishes payload (null-terminated JSON) and returns true on success
        virtual bool publish(const char* payload) = 0;

    protected:
        // Objects are never deleted through this interface, so no virtual destructor is needed
        ~IMqttPublisher() = default;
};
