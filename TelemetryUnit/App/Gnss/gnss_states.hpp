#pragma once

enum class GnssState
{
    GnssIdle,       // startet die GNSS-Aktivierung
    GnssEnabling,   // GNSS eingeschaltet, startet die NMEA-Positionsausgabe
    GnssReady,      // fragt zyklisch den aktuellen Fix ab
    GnssError       // länger als 60s kein Fix, GNSS wird neu initialisiert
};
