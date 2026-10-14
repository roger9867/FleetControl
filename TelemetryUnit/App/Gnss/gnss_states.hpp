#pragma once

enum class GnssState
{
    GnssIdle,       // starts the GNSS activation
    GnssEnabling,   // GNSS switched on, starts the NMEA position output
    GnssReady,      // periodically queries the current fix
    GnssError       // no fix for more than 60s, GNSS is re-initialized
};
