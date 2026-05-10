#pragma once

class BlinkCounter
{
public:
    BlinkCounter();

    void increment();

    int getValue() const;

private:
    int value;
};