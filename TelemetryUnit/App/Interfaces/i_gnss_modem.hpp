#pragma once

#include <cstdint>

// GNSS functions of the modem, as needed by GnssFSM
class IGnssModem
{
    public:
        // Switches GNSS on
        virtual bool activate_gnss() = 0;

        // Starts the NMEA position output
        virtual bool gnss_nmea_start() = 0;

        // Queries the current GNSS position into buffer and returns true if a valid fix is available
        virtual bool get_gnss_fix(uint8_t* buffer, uint16_t buffer_length) = 0;

    protected:
        ~IGnssModem() = default;
};
