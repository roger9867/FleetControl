#pragma once

enum class NetworkState
{
    NetworkIdle,           // startet den Verbindungsaufbau
    NetworkConnecting,     // wartet auf SIM/Signal/APN/PDP und damit auf eine IP
    NetworkReady,          // hat eine IP, baut MQTT-Verbindung zum Broker auf
    NetworkMqttConnected,  // MQTT verbunden, ueberwacht die Verbindung im Hintergrund
    NetworkError           // Verbindung verloren oder Aufbau fehlgeschlagen, faellt zurueck auf Idle
};
