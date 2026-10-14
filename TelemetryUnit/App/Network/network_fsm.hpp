#pragma once

#include <cstdint>

#include "./network_states.hpp"
#include "../Interfaces/i_clock.hpp"
#include "../Interfaces/i_logger.hpp"
#include "../Interfaces/i_network_modem.hpp"
#include "../Interfaces/i_network_status.hpp"

// Establishes and monitors the cellular and MQTT connection
class NetworkFSM : public INetworkStatus
{
    public:
        NetworkState get_state();

        // One step of the FSM, must be called periodically
        void step();

        // INetworkStatus
        bool is_mqtt_connected() const override;
        void report_publish_result(bool ok) override;

        NetworkFSM(INetworkModem& _modem, IClock& _clock, ILogger& _logger);

    private:
        NetworkState current_state = NetworkState::NetworkIdle;

        // Dependencies, injected via the constructor
        INetworkModem& modem;
        IClock& clock;
        ILogger& logger;

        uint32_t last_health_check = 0;
        uint8_t consecutive_publish_failures = 0;
};
