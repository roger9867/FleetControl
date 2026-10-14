#pragma once

#include <cstdint>

#include "../Interfaces/i_clock.hpp"
#include "../Interfaces/i_device_id.hpp"


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
        TelemetryPoint(IClock& _clock, IDeviceId& _device_id);

        // Parses an AT+CGNSSINFO response and updates the stored telemetry data
        TelemetryData parse_gnss(const char* sim_response);

        // Builds the current telemetry point as a JSON string for the MQTT publish
        const char* to_json();

    private:
        // Dependencies, injected via the constructor
        IClock& clock;
        IDeviceId& device_id;

        TelemetryData gnss_data;

        float prev_speed_kmh = 0.0f;
        uint32_t prev_tick_ms = 0;

        // Converts an NMEA coordinate (ddmm.mmmmmm) into decimal degrees
        float nmea_to_decimal(const char* nmea);

        // Builds an ISO 8601 timestamp from the NMEA date/time fields
        void format_timestamp(char* out, unsigned out_len);

        // Calculates the acceleration from the change in speed since the last fix
        float calculate_accel_in_ms2(float new_speed_kmh);

        // Checks whether an NMEA field looks like a valid coordinate
        uint8_t is_valid_nmea_coordinate(const char* token, int min_digits_before_decimal);
};
