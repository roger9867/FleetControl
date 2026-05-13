#pragma once

#include "../../Platform/SIM7600EH/sim7600eh.hpp"

enum class NetworkState
{
    NetworkIdle,
    NetworkConnecting,
    NetworkReady,
    NetworkMqttConnected,
    NetworkError
};


class NetworkFSM
{
    public:
        NetworkState get_state();
        void step();

        explicit NetworkFSM(Sim7600& _network_module);

    private:
        NetworkState current_state = NetworkState::NetworkIdle;

        Sim7600& network_module;    // referenz, da keine Null probleme
};
