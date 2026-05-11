#include "sim7600eh.hpp"
#include <cstring>
#include <stdio.h>


Sim7600::Sim7600(Uart* _huart_sim, Uart* _huart_debug) : 
    huart_sim(_huart_sim),
    huart_debug (_huart_debug)
{
}


bool Sim7600::lte_init()
{
    if (check_connection())
    {
        return true;
    }
}

bool Sim7600::check_connection()
{
    uint8_t at_cmd[] = "AT\r\n";
    huart_sim->write(at_cmd, sizeof(at_cmd)-1);

    uint8_t at_response[8] = {0};

    if (huart_sim->read(at_response, sizeof(at_response), 2000) != HAL_OK)
    {
        return false;
    }

    at_response[sizeof(at_response)-1] = '\0';

    huart_debug->write(at_response, strlen((char*)at_response));

    if (strstr((char*)at_response, "OK") != nullptr)
    {
        return true;
    }

    return false;
}


bool Sim7600::check_sim()
{
    uint8_t cmd[] = "AT+CPIN?\r\n";
    uint8_t response[128];

    // RX flushen
    uint8_t c;
    while (huart_sim->read(&c, 1, 10) == HAL_OK) {}

    // Command senden
    huart_sim->write(cmd, sizeof(cmd) - 1);

    // Antwort lesen
    if (huart_sim->read(response, sizeof(response), 1000) != HAL_OK)
    {
        uint8_t msg[] = "[SIM] No response\r\n";
        huart_debug->write(msg, sizeof(msg) - 1);

        return false;
    }

    // Antwort ausgeben
    huart_debug->write(response, strlen((char*)response));

    uint8_t newline[] = "\r\n";
    huart_debug->write(newline, sizeof(newline) - 1);

    // Auf READY prüfen
    if (strstr((char*)response, "READY") != nullptr)
    {
        uint8_t ok[] = "[SIM] SIM detected\r\n";
        huart_debug->write(ok, sizeof(ok)-1);

        return true;
    }

    uint8_t fail[] = "[SIM] SIM NOT detected\r\n";
    huart_debug->write(fail, sizeof(fail) - 1);

    return false;
}


bool Sim7600::check_network()
{
    uint8_t cmd[] = "AT+CREG?\r\n";
    uint8_t response[128];

    // RX flushen
    uint8_t c;
    while (huart_sim->read(&c, 1, 10) == HAL_OK) {}

    // AT Command senden
    huart_sim->write(cmd, sizeof(cmd) - 1);

    // Antwort lesen
    if (huart_sim->read(response, sizeof(response), 2000) != HAL_OK)
    {
        uint8_t msg[] = "[NET] No response\r\n";
        huart_debug->write(msg, sizeof(msg) - 1);
        return false;
    }

    response[sizeof(response) - 1] = '\0';

    // Debug Ausgabe
    huart_debug->write(response, strlen((char*)response));

    uint8_t nl[] = "\r\n";
    huart_debug->write(nl, sizeof(nl) - 1);

    // Status auswerten
    bool registered =
        (strstr((char*)response, "+CREG: 0,1") != nullptr) ||
        (strstr((char*)response, "+CREG: 0,5") != nullptr);

    if (registered)
    {
        uint8_t ok[] = "[NET] REGISTERED\r\n";
        huart_debug->write(ok, sizeof(ok) - 1);
        return true;
    }

    uint8_t fail[] = "[NET] NOT REGISTERED\r\n";
    huart_debug->write(fail, sizeof(fail) - 1);

    return false;
}

bool Sim7600::check_signal()
{
    uint8_t cmd[] = "AT+CSQ\r\n";
    uint8_t response[128];

    uint8_t c;
    while (huart_sim->read(&c, 1, 10) == HAL_OK) {}

    huart_sim->write(cmd, sizeof(cmd) - 1);

    if (huart_sim->read(response, sizeof(response), 2000) != HAL_OK)
    {
        huart_debug->write((uint8_t*)"[CSQ] No response\r\n", 21);
        return false;
    }

    response[sizeof(response) - 1] = '\0';

    huart_debug->write(response, strlen((char*)response));

    // --------------------------------------------------
    // gezielt CSQ-Zeile finden
    // --------------------------------------------------
    char* p = strstr((char*)response, "+CSQ:");

    if (!p)
    {
        huart_debug->write((uint8_t*)"\r\n[CSQ] Parse error\r\n", 24);
        return false;
    }

    int rssi = 0;
    int ber = 0;

    if (sscanf(p, "+CSQ: %d,%d", &rssi, &ber) == 2)
    {
        if (rssi == 99)
        {
            huart_debug->write((uint8_t*)"\r\n[CSQ] No signal\r\n", 22);
            return false;
        }

        if (rssi >= 10)
        {
            huart_debug->write((uint8_t*)"\r\n[CSQ] Good signal\r\n", 23);
            return true;
        }

        huart_debug->write((uint8_t*)"\r\n[CSQ] Weak signal\r\n", 23);
        return false;
    }

    huart_debug->write((uint8_t*)"\r\n[CSQ] Parse error\r\n", 24);
    return false;
}

bool Sim7600::check_attach()
{
    uint8_t cmd[] = "AT+CGATT?\r\n";
    uint8_t response[128];

    // RX flushen
    uint8_t c;
    while (huart_sim->read(&c, 1, 10) == HAL_OK) {}

    // Command senden
    huart_sim->write(cmd, sizeof(cmd) - 1);

    // Antwort lesen
    if (huart_sim->read(response, sizeof(response), 2000) != HAL_OK)
    {
        huart_debug->write((uint8_t*)"[CGATT] No response\r\n", 23);
        return false;
    }

    response[sizeof(response) - 1] = '\0';

    // Debug Ausgabe
    huart_debug->write(response, strlen((char*)response));

    uint8_t nl[] = "\r\n";
    huart_debug->write(nl, sizeof(nl) - 1);

    // --------------------------------------------------
    // Auswertung
    // Format: +CGATT: 1
    // --------------------------------------------------

    char* p = strstr((char*)response, "+CGATT:");

    if (!p)
    {
        huart_debug->write((uint8_t*)"[CGATT] Parse error\r\n", 23);
        return false;
    }

    int state = 0;

    if (sscanf(p, "+CGATT: %d", &state) == 1)
    {
        if (state == 1)
        {
            huart_debug->write((uint8_t*)"[CGATT] ATTACHED\r\n", 21);
            return true;
        }
        else
        {
            huart_debug->write((uint8_t*)"[CGATT] NOT attached\r\n", 24);
            return false;
        }
    }

    huart_debug->write((uint8_t*)"[CGATT] Parse error\r\n", 23);
    return false;
}

bool Sim7600::set_apn(const char* apn)
{
    uint8_t cmd[64];
    uint8_t response[128];

    // AT Command bauen
    snprintf((char*)cmd, sizeof(cmd),
             "AT+CGDCONT=1,\"IP\",\"%s\"\r\n", apn);

    // RX flushen
    uint8_t c;
    while (huart_sim->read(&c, 1, 10) == HAL_OK) {}

    // Senden
    huart_sim->write(cmd, strlen((char*)cmd));

    // Antwort lesen
    if (huart_sim->read(response, sizeof(response), 2000) != HAL_OK)
    {
        huart_debug->write((uint8_t*)"[APN] No response\r\n", 20);
        return false;
    }

    response[sizeof(response) - 1] = '\0';

    // Debug Ausgabe
    huart_debug->write(response, strlen((char*)response));

    uint8_t nl[] = "\r\n";
    huart_debug->write(nl, sizeof(nl) - 1);

    // OK prüfen
    if (strstr((char*)response, "OK"))
    {
        huart_debug->write((uint8_t*)"[APN] SET OK\r\n", 15);
        return true;
    }

    huart_debug->write((uint8_t*)"[APN] FAILED\r\n", 16);
    return false;
}

bool Sim7600::activate_pdp()
{
    uint8_t cmd[] = "AT+CGACT=1,1\r\n";
    uint8_t response[128];

    // RX flushen
    uint8_t c;
    while (huart_sim->read(&c, 1, 10) == HAL_OK) {}

    // Befehl senden
    huart_sim->write(cmd, sizeof(cmd) - 1);

    // Antwort lesen
    if (huart_sim->read(response, sizeof(response), 5000) != HAL_OK)
    {
        huart_debug->write((uint8_t*)"[PDP] No response\r\n", 20);
        return false;
    }

    response[sizeof(response) - 1] = '\0';

    // Debug Ausgabe
    huart_debug->write(response, strlen((char*)response));

    uint8_t nl[] = "\r\n";
    huart_debug->write(nl, sizeof(nl) - 1);

    // Erfolg prüfen
    if (strstr((char*)response, "OK") != nullptr)
    {
        huart_debug->write((uint8_t*)"[PDP] ACTIVE\r\n", 15);
        return true;
    }

    huart_debug->write((uint8_t*)"[PDP] FAILED\r\n", 16);
    return false;
}


bool Sim7600::get_ip(char* out_ip, uint16_t max_len)
{
    uint8_t cmd[] = "AT+CGPADDR\r\n";
    uint8_t response[128];

    // RX flushen
    uint8_t c;
    while (huart_sim->read(&c, 1, 10) == HAL_OK) {}

    // Senden
    huart_sim->write(cmd, sizeof(cmd) - 1);

    // Antwort lesen
    if (huart_sim->read(response, sizeof(response), 3000) != HAL_OK)
    {
        huart_debug->write((uint8_t*)"[IP] No response\r\n", 20);
        return false;
    }

    response[sizeof(response) - 1] = '\0';

    // Debug output
    huart_debug->write(response, strlen((char*)response));

    uint8_t nl[] = "\r\n";
    huart_debug->write(nl, sizeof(nl) - 1);

    // --------------------------------------------------
    // Beispiel Antwort:
    // +CGPADDR: 1,10.123.45.67
    // --------------------------------------------------

    char* p = strstr((char*)response, "+CGPADDR:");

    if (!p)
    {
        huart_debug->write((uint8_t*)"[IP] Parse error\r\n", 19);
        return false;
    }

    int context = 0;

    // IP Teil finden
    char* ip_start = strchr(p, ',');

    if (!ip_start)
    {
        huart_debug->write((uint8_t*)"[IP] No IP found\r\n", 20);
        return false;
    }

    ip_start++; // hinter ','

    // IP kopieren
    strncpy(out_ip, ip_start, max_len - 1);
    out_ip[max_len - 1] = '\0';

    // Zeilenende entfernen
    char* end = strchr(out_ip, '\r');
    if (end) *end = '\0';

    huart_debug->write((uint8_t*)"[IP] OK: ", 9);
    huart_debug->write((uint8_t*)out_ip, strlen(out_ip));
    huart_debug->write((uint8_t*)"\r\n", 2);

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
    huart_sim->write(cmd1, sizeof(cmd1)-1);
    HAL_Delay(1000);

    // GPS Start
    huart_sim->write(cmd2, sizeof(cmd2)-1);
    HAL_Delay(1000);

    return true;
}

bool Sim7600::gnss_nmea_start()
{
    uint8_t cmd[] = "AT+CGNSTST=1\r\n";
    huart_sim->write(cmd, sizeof(cmd)-1);
    return true;
}



const char* Sim7600::get_gnss()
{
    static uint8_t buffer[256];

    uint8_t cmd[] = "AT+CGNSSINFO\r\n";

    // alten Inhalt löschen
    memset(buffer, 0, sizeof(buffer));

    // Command senden
    huart_sim->write(cmd, sizeof(cmd) - 1);

    // Antwort lesen
    huart_sim->read(buffer, sizeof(buffer), 5000);

    // Debug Ausgabe
    //huart_debug->write(buffer, strlen((char*)buffer));

    uint8_t msg[] = "\r\n[GNSS INFO DONE]\r\n";
    //huart_debug->write(msg, sizeof(msg) - 1);

    return (const char*)buffer;
}