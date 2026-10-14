#pragma once

enum class NetworkState
{
    NetworkIdle,           // starts the connection setup
    NetworkConnecting,     // waits for SIM/signal/APN/PDP and thus for an IP
    NetworkReady,          // has an IP, establishes the MQTT connection to the broker
    NetworkMqttConnected,  // MQTT connected, monitors the connection in the background
    NetworkError           // Connection lost or setup failed, falls back to Idle
};
