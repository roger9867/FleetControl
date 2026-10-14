#pragma once

// Access to the current GNSS state and the latest raw position data
class IGnssSource
{
    public:
        // True once GNSS is enabled and NMEA output is running
        virtual bool is_ready() const = 0;

        // True if the latest query returned a valid position fix
        virtual bool has_valid_fix() const = 0;

        // Latest raw AT+CGNSSINFO response, for parsing by TelemetryPoint
        virtual const char* last_response() const = 0;

    protected:
        ~IGnssSource() = default;
};
