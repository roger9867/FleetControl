#pragma once

#include "../Uart.hpp"
#include "cmsis_os.h"

extern osMutexId_t huart_sim_mutexHandle;
extern osMutexId_t huart_debug_mutexHandle;

// Treiber fuer das SIM7600EH-Modul:
// Beeinhaltet die direkte Kommunikation mit
// dem Waveshare SIM7600E-H LTE/GNSS-Modul
class Sim7600
{
    public:
        Sim7600() {}

        // Prüft, ob eine SIM-Karte eingelegt und entsperrt ist (AT+CPIN?)
        bool check_sim();

        // Prueft die Netzanmeldung beim Mobilfunkanbieter (AT+CREG?)
        bool check_network();

        //Prüft die Signalqualitaet (AT+CSQ)
        bool check_signal();

        // Prüft das Attachment am Paketdatendienst (AT+CGATT?)
        bool check_attach();

        // Setzt den APN fuer die PDP-Kontext-Aktivierung
        bool set_apn();

        // Aktiviert den PDP-Kontext, also die eigentliche Datenverbindung
        bool activate_pdp();

        // Fragt die aktuell zugewiesene IP-Adresse ab (AT+CGPADDR)
        bool get_ip(char* out_ip, uint16_t max_len);

        // Führt alle Schritte fuer den Netzwerkaufbau nacheinander aus
        void establish_network_connection();

        // Liefert true, sobald eine IP-Adresse zugewiesen wurde
        bool is_network_ready();


        // Schaltet GNSS ein
        bool activate_gnss();

        // Startet die NMEA-Positionsausgabe
        bool gnss_nmea_start();
        void get_gnss(uint8_t* buffer, uint16_t buffer_length);
        bool has_fix();

        // Fragt die aktuelle GNSS-Position ab (AT+CGNSSINFO) und liefert
        // true, falls ein gueltiger Fix vorliegt
        bool get_gnss_fix(uint8_t* buffer, uint16_t buffer_length);


        // Startet den MQTT-Client des Moduls (AT+CMQTTSTART)
        bool mqtt_start();

        // Legt eine MQTT-Client-Instanz an (AT+CMQTTACCQ)
        bool mqtt_create_client();

        // Baut die Verbindung zum konfigurierten Broker auf
        bool mqtt_connect();

        // Trennt die Verbindung zum Broker sauber
        bool mqtt_disconnect();

        // Veöffentlicht payload als JSON-Datenpunkt auf der konfigurierten Topic
        bool mqtt_publish(const char* payload);

        bool is_mqtt_connected();

        // Stoppt den MQTT-Client des Moduls
        bool mqtt_stop();


        Uart huart_sim = Uart(&huart3, huart_sim_mutexHandle);
        Uart huart_debug = Uart(&huart2, huart_debug_mutexHandle);

    private:
        // MQTT Credentials
        const char* APN_TOKEN = "internet";
        const char* TOPIC_NAME = "drivedata";
        const char* IP_BROKER = "74.248.232.214";
        const char* MQTT_USER = "mqttuser";
        const char* MQTT_PASSWORD = "hihohihe1967";
        uint32_t PORT_BROKER = 1883;

        // Hilfsfunktion
        void memset(uint8_t *buf, uint8_t value, uint32_t len);
        //bool wait_for_char(char token, uint32_t timeout);
};
