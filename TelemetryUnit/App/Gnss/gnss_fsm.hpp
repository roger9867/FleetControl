#pragma once

#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "./gnss_states.hpp"



class GnssFSM
{
    public:
        GnssState get_state();
        void step();

        const char* get_last_response();
        bool has_valid_fix();

        explicit GnssFSM(Sim7600& _gnss_module);

    private:
        GnssState current_state = GnssState::GnssIdle;

        Sim7600& gnss_module;    // referenz, da keine Null probleme

        char last_response[160] = {0};
        bool last_fix_valid = false;
};
