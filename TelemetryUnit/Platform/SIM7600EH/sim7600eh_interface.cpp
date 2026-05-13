#include "sim7600eh_interface.h"
#include "sim7600eh.hpp"
#include "usart.h"

static Sim7600 sim;

extern "C" bool check_connection()
{
    return sim.check_connection();
}

extern "C" bool sim_check()
{
    return sim.check_sim() ? true : false;
}

extern "C" bool check_network()
{
    return sim.check_network() ? true : false;
}

extern "C" bool check_signal()
{
    return sim.check_signal() ? true : false;
}

extern "C" bool check_attach()
{
    return sim.check_attach() ? true : false;
}

extern "C" bool set_apn()
{
    return sim.set_apn() ? true : false;
}

extern "C" bool activate_pdp()
{
    return sim.activate_pdp() ? true : false;
}

extern "C" bool get_ip(char* out_ip, uint16_t max_len)
{
    return sim.get_ip(out_ip, max_len) ? true : false;
}

////////////////////////////////////////////////////////////////////////
/*
extern "C" bool activate_gnss()
{
    return sim.activate_gnss() ? true : false;
}

extern "C" bool gnss_nmea_start()
{
    return sim.gnss_nmea_start() ? true : false;
}

extern "C" const char* get_gnss()
{
    return sim.get_gnss();
}
*/