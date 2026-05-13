#include "at_buffer.hpp"



void AtBuffer::push(uint8_t c)
{
    if (idx < sizeof(buf) - 1)
    {
        buf[idx++] = c;
        buf[idx] = '\0';
    }
}

bool AtBuffer::has_line()
{
    for (uint16_t i = 0; i < idx; i++)
    {
        if (buf[i] == '\n')
            return true;
    }
    return false;
}

const char* AtBuffer::get_line()
{
    static char line[128];

    uint16_t i = 0;

    while (i < idx && i < sizeof(line) - 1)
    {
        char c = buf[i];

        line[i++] = c;

        if (c == '\n')
            break;
    }

    line[i] = '\0';

    // Buffer nach vorne schieben
    uint16_t remaining = idx - i;
    memmove(buf, buf + i, remaining);
    idx = remaining;

    return line;
}