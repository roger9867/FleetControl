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
        void start_fsm();

    private:
        TelemetryState current_state = TelemetryState::Idle;
        TelemetryState next_state = TelemetryState::Idle;
};
