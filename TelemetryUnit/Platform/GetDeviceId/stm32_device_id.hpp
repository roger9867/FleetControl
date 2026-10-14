#pragma once

#include "../../App/Interfaces/i_device_id.hpp"
#include "device_id_as_uuid.h"

// IDeviceId that reads the STM32 hardware UID once and caches it as UUID string
class Stm32DeviceId : public IDeviceId
{
    public:
        const char* uuid() override
        {
            if (cached[0] == '\0')
                stm32_get_uid_as_uuid(cached);
            return cached;
        }

    private:
        char cached[37] = {0};
};