#pragma once

#include <cstdint>


struct TelemetryData
{
    double latitude;
    double longitude;

    float speed_in_kmh;
    float accel_in_ms2 = 0.0f;

    char date[16];
    char time[16];

    bool valid;
};


class TelemetryPoint
{
    public:
        // Parst eine AT+CGNSSINFO-Antwort und aktualisiert die gespeicherten Telemetriedaten
        TelemetryData parse_gnss(const char* sim_response);

        // Baut den aktuellen Telemetriepunkt als JSON-String fuer den MQTT-Publish
        const char* to_json();

    private:
        TelemetryData gnss_data;
        char device_id[37] = {0};

        float prev_speed_kmh = 0.0f;
        uint32_t prev_tick_ms = 0;

        // Wandelt eine NMEA-Koordinate (ddmm.mmmmmm) in Dezimalgrad um
        float nmea_to_decimal(const char* nmea);

        // Baut aus dem NMEA-Datum/Zeit-Feldern einen ISO-8601-Zeitstempel
        void format_timestamp(char* out, unsigned out_len);

        // Berechnet die Beschleunigung aus der Geschwindigkeitsänderung seit dem letzten Fix
        float calculate_accel_in_ms2(float new_speed_kmh);

        // Prüft, ob ein NMEA-Feld wie eine gültige Koordinate aussieht
        uint8_t is_valid_nmea_coordinate(const char* token, int min_digits_before_decimal);
};
