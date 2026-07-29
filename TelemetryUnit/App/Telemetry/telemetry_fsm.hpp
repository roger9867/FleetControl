#pragma once

enum class TelemetryState
{
    Idle,
    Ready,
    Driving,
    Error
};


class TelemetryFsm
{
    public:
        void fsm_step();

    private:
        TelemetryState current_state = TelemetryState::Idle;
        TelemetryState next_state = TelemetryState::Idle;

        bool is_network_ready();
        bool is_gnss_ready();
};
