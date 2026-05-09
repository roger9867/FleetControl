#include "sim7600eh.h"

void my_memset(uint8_t *buf, uint8_t value, uint32_t len)
{
    for (uint32_t i = 0; i < len; i++)
    {
        buf[i] = value;
    }
}

void confirm_connection(UART_HandleTypeDef* pc,
                        UART_HandleTypeDef* sim)
{
    uint8_t cmd[] = "AT\r\n";
    uint8_t c;
    uint8_t buffer[80];
    uint8_t idx = 0;

    // --------------------------------------------------
    // RX komplett flushen (WICHTIG)
    // --------------------------------------------------
    while (HAL_UART_Receive(sim, &c, 1, 10) == HAL_OK) {}

    // --------------------------------------------------
    // AT senden
    // --------------------------------------------------
    HAL_UART_Transmit(sim, cmd, sizeof(cmd) - 1, 1000);

    // --------------------------------------------------
    // Response sammeln
    // --------------------------------------------------
    uint32_t start = HAL_GetTick();
    uint32_t timeout = 2000;

    while ((HAL_GetTick() - start) < timeout &&
           idx < sizeof(buffer) - 1)
    {
        if (HAL_UART_Receive(sim, &c, 1, 100) == HAL_OK)
        {
            buffer[idx++] = c;



            // --------------------------------------------------
            // stabile End-Erkennung (nicht CRLF!)
            // --------------------------------------------------
            if (idx >= 2)
            {
                // OK erkannt
                if (buffer[idx-2] == 'O' && buffer[idx-1] == 'K')
                    break;

                // ERROR erkannt
                if (idx >= 5 &&
                    buffer[idx-5] == 'E' &&
                    buffer[idx-4] == 'R' &&
                    buffer[idx-3] == 'R')
                    break;
            }
        }
    }

    buffer[idx] = '\0';

    HAL_UART_Transmit(pc, buffer, idx, HAL_MAX_DELAY);

    uint8_t msg[] = "\r\n[AT DONE]\r\n";
    HAL_UART_Transmit(pc, msg, sizeof(msg) - 1, 1000);
}


void sim7600_gnss_on(UART_HandleTypeDef* sim)
{
    uint8_t cmd1[] = "AT+CGNSSPWR=1\r\n";
    uint8_t cmd2[] = "AT+CGPS=1\r\n";

    HAL_UART_Transmit(sim, cmd1, sizeof(cmd1) - 1, HAL_MAX_DELAY);
    osDelay(1000);

    HAL_UART_Transmit(sim, cmd2, sizeof(cmd2) - 1, HAL_MAX_DELAY);
    osDelay(1000);
}

void sim7600_gnss_status(UART_HandleTypeDef* pc,
                         UART_HandleTypeDef* sim)
{
    uint8_t cmd[] = "AT+CGNSSINFO\r\n";
    uint8_t buf[128];
    uint8_t c;
    uint16_t i = 0;

    // RX flush
    while (HAL_UART_Receive(sim, &c, 1, 10) == HAL_OK) {}

    HAL_UART_Transmit(sim, cmd, sizeof(cmd) - 1, HAL_MAX_DELAY);

    uint32_t start = HAL_GetTick();

    while ((HAL_GetTick() - start) < 3000 &&
           i < sizeof(buf) - 1)
    {
        if (HAL_UART_Receive(sim, &c, 1, 100) == HAL_OK)
        {
            buf[i++] = c;

            if (i >= 2 &&
                buf[i-2] == 'O' &&
                buf[i-1] == 'K')
                break;
        }
    }

    buf[i] = '\0';

    HAL_UART_Transmit(pc, buf, i, HAL_MAX_DELAY);
    uint8_t msg[] = "\r\n[GNSS INFO]\r\n";
    HAL_UART_Transmit(pc, msg, sizeof(msg) - 1, HAL_MAX_DELAY);
}

void sim7600_gnss_nmea_start(UART_HandleTypeDef* sim)
{
    uint8_t cmd[] = "AT+CGNSTST=1\r\n";
    HAL_UART_Transmit(sim, cmd, sizeof(cmd) - 1, HAL_MAX_DELAY);
}

void sim7600_gnss_stop(UART_HandleTypeDef* sim)
{
    uint8_t cmd[] = "AT+CGNSTST=0\r\n";
    HAL_UART_Transmit(sim, cmd, sizeof(cmd) - 1, HAL_MAX_DELAY);
}

void sim7600_get_gps(UART_HandleTypeDef* pc,
                       UART_HandleTypeDef* sim)
{
    uint8_t cmd[] = "AT+CGNSSINFO\r\n";

    uint8_t buffer[256];
    uint8_t c;
    uint16_t idx = 0;

    // --------------------------------------------------
    // RX flush
    // --------------------------------------------------
    while (HAL_UART_Receive(sim, &c, 1, 10) == HAL_OK) {}

    // --------------------------------------------------
    // Command senden
    // --------------------------------------------------
    HAL_UART_Transmit(sim, cmd, sizeof(cmd) - 1, HAL_MAX_DELAY);

    // --------------------------------------------------
    // Antwort sammeln
    // --------------------------------------------------
    uint32_t start = HAL_GetTick();
    uint32_t timeout = 5000;

    while ((HAL_GetTick() - start) < timeout &&
           idx < sizeof(buffer) - 1)
    {
        if (HAL_UART_Receive(sim, &c, 1, 100) == HAL_OK)
        {
            buffer[idx++] = c;

            // Ende bei "OK"
            if (idx >= 2)
            {
                if (buffer[idx - 2] == 'O' &&
                    buffer[idx - 1] == 'K')
                {
                    break;
                }
            }

            // Ende bei "ERROR"
            if (idx >= 5)
            {
                if (buffer[idx - 5] == 'E' &&
                    buffer[idx - 4] == 'R' &&
                    buffer[idx - 3] == 'R' &&
                    buffer[idx - 2] == 'O' &&
                    buffer[idx - 1] == 'R')
                {
                    break;
                }
            }
        }
    }

    // --------------------------------------------------
    // String terminieren
    // --------------------------------------------------
    buffer[idx] = '\0';

    // --------------------------------------------------
    // Ausgabe an PC
    // --------------------------------------------------
    HAL_UART_Transmit(pc, buffer, idx, HAL_MAX_DELAY);

    uint8_t msg[] = "\r\n[GNSS INFO DONE]\r\n";
    HAL_UART_Transmit(pc, msg, sizeof(msg) - 1, HAL_MAX_DELAY);
}