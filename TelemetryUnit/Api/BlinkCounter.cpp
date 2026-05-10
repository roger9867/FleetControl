#include "BlinkCounter.hpp"

BlinkCounter::BlinkCounter()
    : value(0)
{
}

void BlinkCounter::increment()
{
    value++;
}

int BlinkCounter::getValue() const
{
    return value;
}