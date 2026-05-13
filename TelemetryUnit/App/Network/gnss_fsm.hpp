#pragma once

#include "../../Platform/SIM7600EH/sim7600eh.hpp"

enum class GnssState
{
    GnssIdle,
    GnssEnabling,
    GnssReady,
    GnssError
};


class GnssFSM
{
    public:
        GnssState get_state();
        void step();

        explicit GnssFSM(Sim7600& _gnss_module);

    private:
        GnssState current_state = GnssState::GnssIdle;

        Sim7600& gnss_module;    // referenz, da keine Null probleme
};
