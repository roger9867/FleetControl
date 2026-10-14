#include <gtest/gtest.h>

#include "fakes.hpp"
#include "../../App/Network/network_fsm.hpp"


// Fixture: every TEST_F gets a fresh network FSM with fakes injected for all dependencies
class NetworkFsmTest : public ::testing::Test
{
    protected:
        FakeNetworkModem modem;
        FakeClock clock;
        FakeLogger logger;

        NetworkFSM fsm{modem, clock, logger};

        // Brings the FSM via Connecting and Ready into MqttConnected
        void enter_mqtt_connected()
        {
            modem.network_ready = true;
            modem.mqtt_ok = true;
            clock.now = 10000;
            fsm.step();     // Idle -> Connecting
            fsm.step();     // Connecting -> Ready
            fsm.step();     // Ready -> MqttConnected
            ASSERT_EQ(fsm.get_state(), NetworkState::NetworkMqttConnected);
        }
};


TEST_F(NetworkFsmTest, MqttConnectFailureGoesToError)
{
    // Given: IP available, but the broker connection fails
    modem.network_ready = true;
    modem.mqtt_ok = false;
    fsm.step();     // Idle -> Connecting
    fsm.step();     // Connecting -> Ready
    ASSERT_EQ(fsm.get_state(), NetworkState::NetworkReady);

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), NetworkState::NetworkError);
    EXPECT_FALSE(fsm.is_mqtt_connected());
}


TEST_F(NetworkFsmTest, ThreeFailedPublishesGoToError)
{
    // Given: connected, two failed publishes reported
    ASSERT_NO_FATAL_FAILURE(enter_mqtt_connected());
    fsm.report_publish_result(false);
    fsm.report_publish_result(false);

    // When / Then: two failures are tolerated
    fsm.step();
    EXPECT_EQ(fsm.get_state(), NetworkState::NetworkMqttConnected);

    // When / Then: the third failure in a row counts as a lost connection
    fsm.report_publish_result(false);
    fsm.step();
    EXPECT_EQ(fsm.get_state(), NetworkState::NetworkError);
}


TEST_F(NetworkFsmTest, HealthCheckDetectsNetworkLoss)
{
    // Given: connected, then the network drops
    ASSERT_NO_FATAL_FAILURE(enter_mqtt_connected());
    modem.network_ready = false;

    // When / Then: no health check yet before 5 s
    clock.now += 4999;
    fsm.step();
    EXPECT_EQ(fsm.get_state(), NetworkState::NetworkMqttConnected);

    // When / Then: the health check after 5 s detects the loss
    clock.now += 1;
    fsm.step();
    EXPECT_EQ(fsm.get_state(), NetworkState::NetworkError);
}
