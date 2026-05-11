#include "Led.hpp"
#include "main.h"


void Led::Led(GPIO_TypeDef* GPIOx, uint16_t GPIO_Pin)
{
    led_gpio_port = GPIOx;
    led_gpio_pin = GPIO_Pin;
}

void Led::on()
{
    HAL_GPIO_WritePin(led_gpio_port, led_gpio_pin, GPIO_PIN_RESET);
}

void Led::off()
{
    HAL_GPIO_WritePin(led_gpio_port, led_gpio_pin, GPIO_PIN_SET);
}

void Led::toggle()
{
    HAL_GPIO_TogglePin(led_gpio_port, led_gpio_pin);
}
