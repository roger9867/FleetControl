#include "telemetry_point.hpp"
#include "../../Platform/SIM7600EH/sim7600eh.hpp"
#include "../../Platform/GetDeviceId/device_id_as_uuid.h"
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

uint8_t TelemetryPoint::is_valid_nmea_coordinate(const char* token, int min_digits_before_decimal)
{
    if (!token || token[0] == '\0')
        return 0;

    const char* dot = strchr(token, '.');
    if (!dot)
        return 0;

    int digits_before_dot = 0;

    for (const char* c = token; c < dot; c++)
    {
        if (*c < '0' || *c > '9')
            return 0;

        digits_before_dot++;
    }

    return digits_before_dot >= min_digits_before_decimal;
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
    bool fields_valid = true;

    char lat_dir = 'N';
    char lon_dir = 'E';

    while (token)
    {
        switch (field)
        {
            case 4: // latitude ddmm.mmmmmm
                if (is_valid_nmea_coordinate(token, 4))
                    out.latitude = nmea_to_decimal(token);
                else
                    fields_valid = false;
                break;

            case 5: // N/S
                lat_dir = token[0];
                break;

            case 6: // longitude dddmm.mmmmmm
                if (is_valid_nmea_coordinate(token, 5))
                    out.longitude = nmea_to_decimal(token);
                else
                    fields_valid = false;
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
                float speed_in_knh = atof(token);
                out.speed_in_kmh = speed_in_knh * 1.852;
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
    if (fields_valid && (out.latitude != 0.0f || out.longitude != 0.0f))
        out.valid = true;

    // Beschleunigung nur zwischen zwei gültigen Fixes berechnen, sonst
    // würde ein Loch (kein Fix) fälschlich als starke Verzögerung gewertet.
    if (out.valid)
        out.accel_in_ms2 = calculate_accel_in_ms2(out.speed_in_kmh);

    gnss_data = out;
    return out;
}


void TelemetryPoint::format_timestamp(char* out, unsigned out_len)
{
    // gnss_data.date is NMEA ddmmyy, gnss_data.time is NMEA hhmmss.sss
    int day = 0, month = 0, year = 0;
    int hour = 0, minute = 0, second = 0;

    sscanf(gnss_data.date, "%2d%2d%2d", &day, &month, &year);
    sscanf(gnss_data.time, "%2d%2d%2d", &hour, &minute, &second);

    snprintf(
        out,
        out_len,
        "20%02d-%02d-%02dT%02d:%02d:%02dZ",
        year, month, day,
        hour, minute, second
    );
}

float TelemetryPoint::calculate_accel_in_ms2(float new_speed_kmh)
{
    uint32_t now = HAL_GetTick();
    float accel = 0.0f;

    if (prev_tick_ms != 0)
    {
        float dt = (now - prev_tick_ms) / 1000.0f;

        if (dt > 0.0f)
        {
            float dv = (new_speed_kmh - prev_speed_kmh) / 3.6f; // km/h -> m/s
            accel = dv / dt;
        }
    }

    prev_speed_kmh = new_speed_kmh;
    prev_tick_ms = now;

    return accel;
}

const char* TelemetryPoint::to_json()
{
    // nicht auf stack -> bss / data, persistent
    static char json[192];
    char timestamp[32];

    if (device_id[0] == '\0')
        stm32_get_uid_as_uuid(device_id);

    format_timestamp(timestamp, sizeof(timestamp));

    snprintf(
        json,
        sizeof(json),
        "{"
        "\"device_id\":\"%s\","
        "\"timestamp_iso_8601\":\"%s\","
        "\"lat\":%.6f,"
        "\"lon\":%.6f,"
        "\"speed_in_kmh\":%.2f,"
        "\"accel_in_ms2\":%.3f"
        "}",
        device_id,
        timestamp,
        gnss_data.latitude,
        gnss_data.longitude,
        gnss_data.speed_in_kmh,
        gnss_data.accel_in_ms2
    );

    return json;
}
