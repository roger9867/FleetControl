#pragma once


struct GnssData
{
    double latitude;
    double longitude;
    float speed_in_kmh;

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
        float nmea_to_decimal(const char* nmea);
};
