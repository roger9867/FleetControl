#include "device_id_as_uuid.h"


void stm32_get_uid_as_uuid(char *out) {
    // Alle drei Teile der Geräte-UID lesen
    uint32_t uid0 = *(uint32_t*)0x1FFFF7E8;
    uint32_t uid1 = *(uint32_t*)0x1FFFF7EC;
    uint32_t uid2 = *(uint32_t*)0x1FFFF7F0;

    char chars_as_hex[25];
    char char_buf[16];

    char* uid_pos = chars_as_hex;

    // Jeden Teil in Hex umwandeln und aneinanderhängen
    i32toStringHex(char_buf, uid0);
    for (char* hex_ptr = char_buf; *hex_ptr;  hex_ptr++) {
        *uid_pos++ = *hex_ptr;
    }

    i32toStringHex(char_buf, uid1);
    for (char* hex_ptr = char_buf; *hex_ptr;  hex_ptr++) {
        *uid_pos++ = *hex_ptr;
    }

    i32toStringHex(char_buf, uid2);
    for (char* hex_ptr = char_buf; *hex_ptr;  hex_ptr++) {
        *uid_pos++ = *hex_ptr;
    }

    // Auf 32 Hex-Zeichen auffüllen
    while ((uid_pos - chars_as_hex) < 32) {
        *uid_pos++ = '0';
    }

    *uid_pos = '\0';

    // '-' für UUID-Format einfügen: 8-4-4-4-12
    int index_hex = 0;
    int index_out = 0;

    for (int i = 0; i < 32; i++) {
        out[index_out++] = chars_as_hex[index_hex++];

        if (i == 7 || i == 11 || i == 15 || i == 19) {
            out[index_out++] = '-';
        }
    }

    out[index_out] = '\0';
}
