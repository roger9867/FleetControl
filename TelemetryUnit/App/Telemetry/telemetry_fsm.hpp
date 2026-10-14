#pragma once

#include "../Interfaces/i_clock.hpp"
#include "../Interfaces/i_gnss_source.hpp"
#include "../Interfaces/i_logger.hpp"
#include "../Interfaces/i_mqtt_publisher.hpp"
#include "../Interfaces/i_network_status.hpp"
#include "telemetry_point.hpp"


// Interval between two telemetry publishes
#define PUBLISH_INTERVAL_MS 1000U


enum class TelemetryState
{
    Idle,       // waits for network and GNSS readiness
    Publishing, // sends telemetry points via MQTT at a fixed interval
    Error       // last publish failed, falls back to Idle
};


// Controls the periodic sending of telemetry data (position, speed,
// acceleration) as soon as the network and GNSS FSMs are ready
class TelemetryFsm
{
    public:
        TelemetryFsm(IMqttPublisher& _mqtt,
             INetworkStatus& _network,
             IGnssSource& _gnss,
             TelemetryPoint& _telemetry_point,
             IClock& _clock,
             ILogger& _logger
        );

        // Executes one step of the FSM, must be called periodically
        void step();
        TelemetryState get_state();

    private:
        TelemetryState current_state = TelemetryState::Idle;

        // Dependencies, injected via the constructor
        IMqttPublisher& mqtt;
        INetworkStatus& network;
        IGnssSource& gnss;
        TelemetryPoint& telemetry_point;
        IClock& clock;
        ILogger& logger;

        uint32_t last_publish = 0;

        bool is_network_ready();
        bool is_gnss_ready();

        // Builds the current telemetry point and sends it via MQTT
        bool publish_timestamp();
};
