#include "sim7600eh.hpp"
#include <cstring>
#include <stdio.h>
#include <stdlib.h>
#include "cmsis_os.h"
#include "FreeRTOS.h"


bool Sim7600::check_connection()
{
    send_cmd("AT\r\n");

    return wait_for("OK", 2000);
}



void Sim7600::establish_network_connection()
{
    check_connection();
    check_sim();
    check_network();
    check_signal();
    check_attach();
    set_apn();
    activate_pdp();
}


void Sim7600::on_uart_rx(uint8_t c)
{
    at_buffer.push(c);
}

bool Sim7600::wait_for(const char* token, uint32_t timeout)
{
    uint32_t start = HAL_GetTick();

    while (HAL_GetTick() - start < timeout)
    {
        if (at_buffer.has_line())
        {
            const char* line = at_buffer.get_line();

            huart_debug.write((uint8_t*)line, strlen(line));

            if (strstr(line, token))
                return true;
        }
    }

    return false;
}

void Sim7600::send_cmd(const char* cmd)
{
    huart_sim.write((uint8_t*)cmd, strlen(cmd));
}


bool Sim7600::is_network_ready()
{
    char ip[32];
    return  Sim7600::get_ip(ip, sizeof(ip));
}



bool Sim7600::check_sim()
{
    uint8_t cmd[] = "AT+CPIN?\r\n";
    uint8_t response[128] = {0};

    //huart_sim.acquire();

    uint8_t c;
    while (huart_sim.read(&c, 1, 10) == HAL_OK) {}

    huart_sim.write(cmd, sizeof(cmd) - 1);

    bool ok = (huart_sim.read(response, sizeof(response) - 1, 1000) == HAL_OK);

    //huart_sim.release();

    if (!ok)
    {
        huart_debug.write((uint8_t*)"[SIM] No response\r\n", 20);
        return false;
    }

    if (strstr((char*)response, "READY"))
    {
        huart_debug.write((uint8_t*)"[SIM] OK\r\n", 10);
        return true;
    }

    huart_debug.write((uint8_t*)"[SIM] FAIL\r\n", 12);
    return false;
}


bool Sim7600::check_network()
{
    uint8_t cmd[] = "AT+CREG?\r\n";
    uint8_t response[128] = {0};

    //huart_sim.acquire();

    uint8_t c;
    while (huart_sim.read(&c, 1, 10) == HAL_OK) {}

    huart_sim.write(cmd, sizeof(cmd) - 1);

    bool ok = (huart_sim.read(response, sizeof(response) - 1, 2000) == HAL_OK);

    //huart_sim.release();

    if (!ok)
    {
        huart_debug.write((uint8_t*)"[NET] No response\r\n", 20);
        return false;
    }

    bool registered =
        strstr((char*)response, ",1") ||
        strstr((char*)response, ",5");

    huart_debug.write((uint8_t*)response, strlen((char*)response));

    return registered;
}

bool Sim7600::check_signal()
{
    uint8_t cmd[] = "AT+CSQ\r\n";
    uint8_t response[128] = {0};

    //huart_sim.acquire();

    uint8_t c;
    while (huart_sim.read(&c, 1, 10) == HAL_OK) {}

    huart_sim.write(cmd, sizeof(cmd) - 1);

    bool ok = (huart_sim.read(response, sizeof(response) - 1, 2000) == HAL_OK);

    //huart_sim.release();

    if (!ok)
    {
        huart_debug.write((uint8_t*)"[CSQ] No response\r\n", 21);
        return false;
    }

    char* p = strstr((char*)response, "+CSQ:");

    if (!p)
    {
        huart_debug.write((uint8_t*)"[CSQ] Parse error\r\n", 21);
        return false;
    }

    p += 6;

    int rssi = atoi(p);

    if (rssi == 99)
    {
        huart_debug.write((uint8_t*)"[CSQ] No signal\r\n", 19);
        return false;
    }

    return (rssi >= 10);
}

bool Sim7600::check_attach()
{
    uint8_t cmd[] = "AT+CGATT?\r\n";
    uint8_t response[128] = {0};

    //huart_sim.acquire();

    uint8_t c;
    while (huart_sim.read(&c, 1, 10) == HAL_OK) {}

    huart_sim.write(cmd, sizeof(cmd) - 1);

    bool ok = (huart_sim.read(response, sizeof(response) - 1, 2000) == HAL_OK);

    //huart_sim.release();

    if (!ok)
    {
        huart_debug.write((uint8_t*)"[CGATT] No response\r\n", 23);
        return false;
    }

    char* p = strstr((char*)response, "+CGATT:");

    if (!p)
        return false;

    return (*(p + 8) == '1');
}

bool Sim7600::set_apn()
{
    uint8_t cmd[64];
    uint8_t response[128] = {0};

    snprintf((char*)cmd, sizeof(cmd),
             "AT+CGDCONT=1,\"IP\",\"%s\"\r\n", APN_TOKEN);

    //huart_sim.acquire();

    uint8_t c;
    while (huart_sim.read(&c, 1, 10) == HAL_OK) {}

    huart_sim.write(cmd, strlen((char*)cmd));

    bool ok = (huart_sim.read(response, sizeof(response) - 1, 2000) == HAL_OK);

    //huart_sim.release();

    return ok && strstr((char*)response, "OK");
}

bool Sim7600::activate_pdp()
{
    uint8_t cmd[] = "AT+CGACT=1,1\r\n";
    uint8_t response[128] = {0};

    //huart_sim.acquire();

    uint8_t c;
    while (huart_sim.read(&c, 1, 10) == HAL_OK) {}

    huart_sim.write(cmd, sizeof(cmd) - 1);

    bool ok = (huart_sim.read(response, sizeof(response) - 1, 5000) == HAL_OK);

    //huart_sim.release();

    return ok && strstr((char*)response, "OK");
}


bool Sim7600::get_ip(char* out_ip, uint16_t max_len)
{
    uint8_t cmd[] = "AT+CGPADDR\r\n";
    uint8_t response[128] = {0};

    //huart_sim.acquire();

    uint8_t c;
    while (huart_sim.read(&c, 1, 10) == HAL_OK) {}

    huart_sim.write(cmd, sizeof(cmd) - 1);

    bool ok = (huart_sim.read(response, sizeof(response) - 1, 3000) == HAL_OK);

    //huart_sim.release();

    if (!ok)
        return false;

    char* p = strstr((char*)response, "+CGPADDR:");

    if (!p)
        return false;

    char* ip = strchr(p, ',');

    if (!ip)
        return false;

    ip++;

    strncpy(out_ip, ip, max_len - 1);
    out_ip[max_len - 1] = '\0';

    char* end = strchr(out_ip, '\r');
    if (end) *end = 0;

    return true;
}




////////////////////////////////////////////////////////////////////////////////////////////////
// gnss
////////////////////////////////////////////////////////////////////////////////////////////////




void Sim7600::memset(uint8_t *buf, uint8_t value, uint32_t len)
{
    for (uint32_t i = 0; i < len; i++)
    {
        buf[i] = value;
    }
}

bool Sim7600::activate_gnss()
{
    
    uint8_t cmd1[] = "AT+CGNSSPWR=1\r\n";
    uint8_t cmd2[] = "AT+CGPS=1\r\n";

    // GNSS Power ON
    //huart_sim.acquire();
    huart_sim.write(cmd1, sizeof(cmd1)-1);
    HAL_Delay(1000);

    // GPS Start
    huart_sim.write(cmd2, sizeof(cmd2)-1);
    HAL_Delay(1000);

    //huart_sim.release();

    return true;
}

bool Sim7600::gnss_nmea_start()
{
    //huart_sim.acquire();
    uint8_t cmd[] = "AT+CGNSTST=1\r\n";
    huart_sim.write(cmd, sizeof(cmd)-1);
    //huart_sim.release();
    return true;
}





void Sim7600::get_gnss(uint8_t* buffer, uint16_t buffer_length)
{
    //huart_sim.acquire();

    uint8_t cmd[] = "AT+CGNSSINFO\r\n";

    // Buffer löschen
    memset(buffer, 0, buffer_length);

    // Command senden
    huart_sim.write(cmd, sizeof(cmd) - 1);

    // Antwort lesen
    huart_sim.read(buffer, buffer_length - 1, 5000);

    // Sicherheit
    buffer[buffer_length - 1] = '\0';
    //huart_sim.release();

    //huart_debug.acquire();
    // Debug Ausgabe
    huart_debug.write(
        buffer,
        strlen((char*)buffer)
    );

    uint8_t msg[] = "\r\n[GNSS INFO DONE]\r\n";

    huart_debug.write(
        msg,
        sizeof(msg) - 1
    );
    //huart_debug.release();
}


bool Sim7600::has_fix()
{
    uint8_t buffer[128] = {0};

    get_gnss(buffer, sizeof(buffer));

    // Prefix finden
    char* p = strstr((char*)buffer, "+CGNSSINFO:");
    //huart_debug.acquire();

    if (p == nullptr)
    {
        
        uint8_t msg[] =
            "[GNSS] PARSE ERROR\r\n";

        huart_debug.write(
            msg,
            sizeof(msg) - 1
        );
        //huart_debug.release();
        return false;
    }

    // Erstes Feld lesen
    int fix_mode = 0;

    if (sscanf(p, "+CGNSSINFO: %d", &fix_mode) != 1)
    {
        uint8_t msg[] =
            "[GNSS] SCAN ERROR\r\n";

        huart_debug.write(
            msg,
            sizeof(msg) - 1
        );

        //huart_debug.release();
        return false;
    }

    // Fix prüfen
    if (fix_mode == 1 || fix_mode == 2)
    {
        uint8_t msg[] =
            "[GNSS] FIX OK\r\n";

        huart_debug.write(
            msg,
            sizeof(msg) - 1
        );
        //huart_debug.release();
        return true;
    }

    uint8_t msg[] =
        "[GNSS] NO FIX\r\n";

    huart_debug.write(
        msg,
        sizeof(msg) - 1
    );

    //huart_debug.release();
    return false;
}
