#pragma once

#include <cstdint>
#include <cstdio>
#include <string>
#include <vector>

#include "../../App/Interfaces/i_clock.hpp"
#include "../../App/Interfaces/i_logger.hpp"
#include "../../App/Interfaces/i_device_id.hpp"
#include "../../App/Interfaces/i_gnss_source.hpp"
#include "../../App/Interfaces/i_network_status.hpp"
#include "../../App/Interfaces/i_mqtt_publisher.hpp"
#include "../../App/Interfaces/i_gnss_modem.hpp"
#include "../../App/Interfaces/i_network_modem.hpp"


class FakeClock : public IClock
{
    public:
        uint32_t get_clock_ticks() override { return now; }
        uint32_t now = 0;
};


class FakeLogger : public ILogger
{
    public:
        void log(const char*) override {}
};


class FakeDeviceId : public IDeviceId
{
    public:
        const char* uuid() override { return "00000000-0000-0000-0000-000000000000"; }
};


class FakeGnssSource : public IGnssSource
{
    public:
        bool is_ready() const override { return ready; }
        bool ready = false;

        bool has_valid_fix() const override { return fix; }
        bool fix = false;
        const char* last_response() const override { return ""; }
};


class FakeNetworkStatus : public INetworkStatus
{
    public:
        bool is_mqtt_connected() const override { return connected; }
        bool connected = false;

        void report_publish_result(bool) override {}
};


class FakeMqttPublisher : public IMqttPublisher
{
    public:
        bool publish(const char* payload) override
        {
            published.emplace_back(payload);
            return next_result;
        }

        bool next_result = true;               // result returned to the FSM
        std::vector<std::string> published;    // every payload sent so far
};


// Spy for the GNSS part of the modem: the test sets whether a fix is available
class FakeGnssModem : public IGnssModem
{
    public:
        bool activate_gnss() override { activate_calls++; return true; }
        bool gnss_nmea_start() override { nmea_start_calls++; return true; }

        bool get_gnss_fix(uint8_t* buffer, uint16_t buffer_length) override
        {
            // copy the configured response so the FSM can cache it
            snprintf((char*)buffer, buffer_length, "%s", response);
            return fix;
        }

        bool fix = false;
        const char* response = "";
        int activate_calls = 0;
        int nmea_start_calls = 0;
};


// Spy for the network part of the modem: the test sets whether IP and MQTT are available
class FakeNetworkModem : public INetworkModem
{
    public:
        void establish_network_connection() override { establish_calls++; }
        bool is_network_ready() override { return network_ready; }
        bool mqtt_start() override { return mqtt_ok; }
        bool mqtt_create_client() override { return mqtt_ok; }
        bool mqtt_connect() override { return mqtt_ok; }
        bool mqtt_disconnect() override { disconnect_calls++; return true; }

        bool network_ready = false;
        bool mqtt_ok = true;
        int establish_calls = 0;
        int disconnect_calls = 0;
};
