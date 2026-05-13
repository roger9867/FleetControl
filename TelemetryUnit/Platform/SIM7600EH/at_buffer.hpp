#pragma once

#include <cstdint>
#include <cstring>

class AtBuffer
{
public:
    void push(uint8_t c);
    bool has_line();

    const char* get_line(); // \r\n terminated
private:
    char buf[512];
    uint16_t idx = 0;
};
