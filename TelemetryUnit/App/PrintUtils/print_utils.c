#include "print_utils.h"


oid uprints(UART_HandleTypeDef* huart, const char* message) {
    uint32_t count = 0;
    char* current_pos = message;
    while (*current_pos != '\0') {
        count++;
        current_pos++;
    }
    HAL_UART_Transmit(huart, (uint8_t *) message,  count, HAL_MAX_DELAY);
}

void u32toStringDec(char* inbuf, uint32_t uint) {
    if (uint == 0) {    // Handle input = 0
        inbuf[0] = '0';
        inbuf[1] = '\0';
        return;
    }
    int i = 0;
    while (uint != 0) {
        inbuf[i] = '0' + (uint % 10);   // Extract base 10 numbers
        uint /= 10;
        i++;
    }
    // Turn characters in buffer 
    for (int j = 0; j < i / 2; ++j) {
        char tmp = inbuf[j];
        inbuf[j] = inbuf[i-1-j];
        inbuf[i-1-j] = tmp;
    }
    inbuf[i] = '\0';    // Limit buffer and add terminator
}

void i32toStringDec(char* inbuf, int32_t _int) {
    if (_int == 0) {    // Handle input = 0
        inbuf[0] = '0';
        inbuf[1] = '\0';
        return;
    }
    int negative = (_int < 0);  // Detect necessary sign

    int i = 0;
    while (_int != 0) {
        if (_int < 0) {
            inbuf[i] = '0' - (_int % 10);   // Extract base ten numbers
        }
        else {
            inbuf[i] = '0' + (_int % 10);   // Extract base ten numbers
        }
        _int /= 10;
        i++;
    }

    // Turn characters in buffer 
    for (int j=0; j<i/2; ++j) {
        char tmp = inbuf[j];
        inbuf[j] = inbuf[i-1-j];
        inbuf[i-1-j] = tmp;
    }
    if (negative) {
        inbuf[i+1] = '\0';  // Limit buffer and add terminator
        while (i>0) {
            inbuf[i] = inbuf[i-1];
            i--;
        }
        inbuf[0] = '-';
    }
    else {
        inbuf[i] = '\0';  // Limit buffer and add terminator
    }
}

void f32toStringDec(char* buf, float val) {
    int32_t int_part = (int32_t)val;                     // integer part
    int32_t frac_part = (int32_t)((val - int_part) * 1000); // 3 figures after komma

    if (frac_part < 0) frac_part = -frac_part;

    char tmp[16];
    int i = 0;

    // Put integer in buffer backwarts
    if (int_part == 0)
        tmp[i++] = '0';
    else {
        int32_t n = int_part;
        if (n < 0) n = -n;

        while (n > 0) {
            tmp[i++] = '0' + (n % 10);
            n /= 10;
        }

        if (int_part < 0)
            tmp[i++] = '-';
    }

    // turn
    int j = 0;
    while (i > 0)
        buf[j++] = tmp[--i];

    // Dezimal point
    buf[j++] = '.';

    // figures after komma
    buf[j++] = '0' + (frac_part / 100);
    buf[j++] = '0' + ((frac_part / 10) % 10);
    buf[j++] = '0' + (frac_part % 10);

    buf[j] = '\0';
}

void i32toStringBin(char* inbuf, int32_t val) {
    // binary conversion
    for (int j = 31, i = 0; i <= 32 && j >= 0; i++, j--) {
        inbuf[j] = '0' + ((val >> i) & 1);
    }
    inbuf[32] = '\0';
}

void u8toStringBin(char* inout_buf, uint8_t val) {
        for (int j = 7, i = 0; i <= 8 && j >= 0; i++, j--) {
        inout_buf[j] = '0' + ((val >> i) & 1);
    }
    inout_buf[8] = '\0';
}


void i32toStringHex(char* inbuf, int32_t _int) {
    for (int i = 0; i < 8; i++) {
        int8_t figure = 0;
        for (int j = 0; j < 4; j++) {
            // Assemble figure with bit mask
            figure += (_int >> (i * 4)) & (1 << j);
        }
        switch(figure) {
            case 10: inbuf[8 - 1 - i] = 'A'; break;
            case 11: inbuf[8 - 1 - i] = 'B'; break;
            case 12: inbuf[8 - 1 - i] = 'C'; break;
            case 13: inbuf[8 - 1 - i] = 'D'; break;
            case 14: inbuf[8 - 1 - i] = 'E'; break;
            case 15: inbuf[8 - 1 - i] = 'F'; break;
            default: inbuf[8 - 1 - i] = '0' + figure; break;
        }
    }
    inbuf[8] = '\0';
}

void adressContentToString(char* inbuf, void* address) {
    int8_t contained_byte = *((int8_t*) address);
    i32toStringHex(inbuf, (int32_t) contained_byte);
}
