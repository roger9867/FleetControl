#pragma once

#include <cstdarg>
#include <cstdio>

// Abstraction of a line-based debug log, so the FSMs do not depend on a UART
class ILogger
{
    public:
        // Outputs one complete log line; the implementation appends the line ending
        virtual void log(const char* line) = 0;

        // printf-style convenience wrapper around log(), formats into a fixed buffer (no heap)
        void logf(const char* format, ...) __attribute__((format(printf, 2, 3)))
        {
            char line[256];
            va_list args;
            va_start(args, format);
            vsnprintf(line, sizeof(line), format, args);
            va_end(args);
            log(line);
        }

    protected:
        ~ILogger() = default;
};
