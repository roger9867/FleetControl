#pragma once

// Provides the unique ID of this telemetry unit
class IDeviceId
{
    public:
        // Returns the device ID as a UUID string (36 characters, null-terminated)
        virtual const char* uuid() = 0;

    protected:
        ~IDeviceId() = default;
};
