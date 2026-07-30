#pragma once

enum class NetworkState
{
    NetworkIdle,
    NetworkConnecting,
    NetworkReady,
    NetworkMqttConnected,
    NetworkError
};
