#pragma once

class Led
{

public:
    void on();
    void off();
    void toggle();

    Led(GPIO_TypeDef* GPIOx, uint16_t GPIO_Pin);

private:
    GPIO_TypeDef* led_gpio_port;
    uint16_t led_gpio_pin;
};