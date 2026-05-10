#include "test.h"

#include "BlinkCounter.hpp"
#include "main.h"

void cpp_test()
{
    BlinkCounter counter;

    counter.increment();

    if(counter.getValue() == 1)
    {
        HAL_GPIO_TogglePin(LD2_GPIO_Port, LD2_Pin);
    }
}
