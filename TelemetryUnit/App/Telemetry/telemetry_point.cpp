#include "telemetry_point.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include <string.h>
#include <stdlib.h>
#include <cstdio>


float TelemetryPoint::nmea_to_decimal(const char* nmea)
{
    float val = atof(nmea);

    int degrees = (int)(val / 100);
    float minutes = val - (degrees * 100);

    return degrees + (minutes / 60.0f);
}

GnssData TelemetryPoint::parse_gnss(const char* response)
{
    GnssData out{};
    out.valid = false;

    if (!response)
        return out;

    // "+CGNSSINFO:" suchen
    const char* p = strstr(response, "+CGNSSINFO:");
    if (!p)
        return out;

    // hinter ':' springen
    p = strchr(p, ':');
    if (!p)
        return out;

    p++;

    // lokale Kopie für strtok
    char buf[256];
    strncpy(buf, p, sizeof(buf));
    buf[sizeof(buf) - 1] = '\0';

    char* token = strtok(buf, ",");

    int field = 0;

    char lat_dir = 'N';
    char lon_dir = 'E';

    while (token)
    {
        switch (field)
        {
            case 4: // latitude ddmm.mmmmmm
                out.latitude = nmea_to_decimal(token);
                break;

            case 5: // N/S
                lat_dir = token[0];
                break;

            case 6: // longitude dddmm.mmmmmm
                out.longitude = nmea_to_decimal(token);
                break;

            case 7: // E/W
                lon_dir = token[0];
                break;

            case 8: // date
                strncpy(out.date, token, sizeof(out.date));
                out.date[sizeof(out.date) - 1] = '\0';
                break;

            case 9: // UTC time
                strncpy(out.time, token, sizeof(out.time));
                out.time[sizeof(out.time) - 1] = '\0';
                break;

            case 11: // speed km/h
                out.speed_in_kmh = atof(token);
                break;
        }

        token = strtok(NULL, ",");
        field++;
    }

    // Süd/West negativ
    if (lat_dir == 'S')
        out.latitude *= -1.0f;

    if (lon_dir == 'W')
        out.longitude *= -1.0f;

    // einfache Validierung
    if (out.latitude != 0.0f || out.longitude != 0.0f)
        out.valid = true;

    return out;
}


const char* TelemetryPoint::to_json()
{
    // nicht auf stack -> bss / data, persistent
    static char json[128];

     snprintf(
        json,
        sizeof(json),
        "{"
        "\"time\":\"%s\","
        "\"lat\":%.6f,"
        "\"lon\":%.6f,"
        "\"speed\":%.2f"
        "}",
        gnss_data.time,
        gnss_data.latitude,
        gnss_data.longitude,
        gnss_data.speed_in_kmh
    );

    return json;
}