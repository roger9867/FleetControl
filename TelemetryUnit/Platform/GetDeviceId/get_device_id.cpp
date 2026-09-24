#include "get_device_id.h"
#include "../PrintUtils/print_utils.h"
#include "../Uart.hpp"


#define CMD_BUF_SIZE 32
#define UUID_SIZE 36


bool is_get_id_command(char* msg_to_check, uint8_t msg_length);


char cmd_buffer[CMD_BUF_SIZE+1];
char uuid_device[UUID_SIZE+1];

static Uart huart_debug_wrapper(&huart2, huart_debug_mutexHandle);


extern "C" void check_for_device_id_request_command(void)
{
    uint8_t byte_read;
    uint8_t bytes_read_count = 0;

    while (true)
    {
        // Ein Byte vom Debug-UART lesen
        if (huart_debug_wrapper.readRaw(&byte_read, 1, HAL_MAX_DELAY) == HAL_OK)
        {
            if (byte_read == '\n' || byte_read == '\r')
            {
                // Zeilenende: leere Zeile ignorieren
                if (bytes_read_count == 0)
                {
                    continue;
                }

                cmd_buffer[bytes_read_count] = '\0';

                // Befehl auswerten und Antwort senden
                if (is_get_id_command(cmd_buffer, bytes_read_count))
                {
                    stm32_get_uid_as_uuid(uuid_device);

                    huart_debug_wrapper.write((uint8_t*)uuid_device, 36);
                    huart_debug_wrapper.write((uint8_t*)"\r\n", 2);
                }
                else
                {
                    huart_debug_wrapper.write((uint8_t*)"Unknown command\r\n", 18);
                }

                // Puffer fuer den nächsten Befehl zuruecksetzen
                bytes_read_count = 0;
            }
            else
            {
                // Byte an den Befehlspuffer anhaengen
                if (bytes_read_count < sizeof(cmd_buffer) - 1)
                {
                    cmd_buffer[bytes_read_count++] = byte_read;
                }
                else
                {
                    // Puffer voll ohne Zeilenende: Befehl verwerfen
                    bytes_read_count = 0;
                }
            }
        }
    }
}

bool is_get_id_command(char* msg_to_check, uint8_t msg_length) {
    const char get_cmd[] = "get_device_id";
    const uint8_t cmd_length = 13;

    if (msg_length != cmd_length)
        return false;

    for (uint8_t i = 0; i < cmd_length; i++) {
        if (msg_to_check[i] != get_cmd[i])
            return false;
    }

    return true;
}
