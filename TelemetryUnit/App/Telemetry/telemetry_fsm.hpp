#pragma once

#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "telemetry_point.hpp"

enum class TelemetryState
{
    Idle,
    Publishing,
    Error
};


class TelemetryFsm
{
    public:
        explicit TelemetryFsm(Sim7600& _telemetry_module);

        void step();
        TelemetryState get_state();

    private:
        TelemetryState current_state = TelemetryState::Idle;

        Sim7600& telemetry_module;
        TelemetryPoint telemetry_point;

        uint32_t last_publish = 0;

        bool is_network_ready();
        bool is_gnss_ready();
        bool publish_timestamp();
};
