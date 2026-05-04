#include "get_device_id.h"


#define CMD_BUF_SIZE 32
#define UUID_SIZE 36


// Checks message buffer for get_device_id command
bool is_get_id_command(char* msg_to_check, uint8_t msg_length);


char cmd_buffer[CMD_BUF_SIZE+1];
char uuid_device[UUID_SIZE+1];


void check_for_device_id_request_command(UART_HandleTypeDef* huartx) {
    uint8_t byte_read;
    uint8_t bytes_read_count = 0;

    while(true) {
        if (HAL_UART_Receive(huartx, &byte_read, 1, HAL_MAX_DELAY) == HAL_OK){

            // Check for possible end line chararcters
            if (byte_read == '\n' || byte_read == '\r') {
                cmd_buffer[bytes_read_count] = '\0';

                if (is_get_id_command(cmd_buffer, bytes_read_count)) {
                    stm32_get_uid_as_uuid(uuid_device);

                    // Send uid as formatted UUID over UART
                    HAL_UART_Transmit(
                        huartx,
                        (uint8_t*)uuid_device,
                        36,
                        HAL_MAX_DELAY
                    );

                    // Clean termination
                    HAL_UART_Transmit(
                        huartx,
                        (uint8_t*)"\r\n",
                        2,
                        HAL_MAX_DELAY
                    );
                }
                else {
                    uprints(huartx, "Unknown command\r\n");
                }

                bytes_read_count = 0;
            }
            else {
                if (bytes_read_count < sizeof(cmd_buffer)-1) {
                    cmd_buffer[bytes_read_count++] = byte_read;
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