#pragma once

enum class GnssState
{
    Idle,
    Enabled,
    Started,
    Error
};


class GnssFsm
{
    public:
        void start_gnss_fsm();
        GnssState get_current_state();

    private:
        GnssState current_state = GnssState::Idle;
        GnssState next_state = GnssState::Idle;
};
