#pragma once

#include <cstdint>


struct GnssData
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
        GnssData parse_gnss(const char* sim_response);
        const char* to_json();

    private:
        GnssData gnss_data;
        char device_id[37] = {0};

        float prev_speed_kmh = 0.0f;
        uint32_t prev_tick_ms = 0;

        float nmea_to_decimal(const char* nmea);
        void format_timestamp(char* out, unsigned out_len);
        float calculate_accel_in_ms2(float new_speed_kmh);
};
