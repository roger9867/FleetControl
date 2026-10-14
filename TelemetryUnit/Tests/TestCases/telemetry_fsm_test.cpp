#include <gtest/gtest.h>

#include "fakes.hpp"
#include "../../App/Telemetry/telemetry_fsm.hpp"


// Fixture: every TEST_F gets a fresh FSM with fakes injected for all dependencies
class TelemetryFsmTest : public ::testing::Test
{
    protected:
        // Fakes must exist
        FakeClock clock;
        FakeLogger logger;
        FakeDeviceId device_id;
        FakeGnssSource gnss;
        FakeNetworkStatus network;
        FakeMqttPublisher mqtt;

        TelemetryPoint point = TelemetryPoint(clock, device_id);

        // Only the following two variants are valid c++
        // TelemetryFsm fsm{mqtt, network, gnss, point, clock, logger};
        TelemetryFsm fsm = TelemetryFsm(mqtt, network, gnss, point, clock, logger);

                // Brings the FSM into Publishing with everything ready and a valid fix
        void enter_publishing()
        {
            network.connected = true;
            gnss.ready = true;
            gnss.fix = true;
            clock.now = 10000;
            fsm.step();
            ASSERT_EQ(fsm.get_state(), TelemetryState::Publishing);
        }
};





TEST_F(TelemetryFsmTest, ShouldStartInIdle)
{
    // Given: a freshly constructed FSM (created by the fixture)

    // When
    TelemetryState start_state = fsm.get_state();   // fsm can be used directly, fixture creates instance

    // Then
    EXPECT_EQ(start_state, TelemetryState::Idle);   // ASSERT_* would abort this test, EXPECT_* continues
}


TEST_F(TelemetryFsmTest, StaysIdleWhenNothingIsReady)
{
    // Given: network and GNSS not ready (fake defaults)

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), TelemetryState::Idle);
}


TEST_F(TelemetryFsmTest, StaysIdleWhenOnlyNetworkIsReady)
{
    // Given
    network.connected = true;

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), TelemetryState::Idle);
}


TEST_F(TelemetryFsmTest, StaysIdleWhenOnlyGnssIsReady)
{
    // Given
    gnss.ready = true;

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), TelemetryState::Idle);
}


TEST_F(TelemetryFsmTest, GoesToPublishingWhenNetworkAndGnssReady)
{
    // Given
    network.connected = true;
    gnss.ready = true;

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), TelemetryState::Publishing);
}


TEST_F(TelemetryFsmTest, PublishesOncePerInterval)
{
    // Given: everything ready, FSM in Publishing
    network.connected = true;
    gnss.ready = true;
    gnss.fix = true;
    clock.now = 10000;
    fsm.step();                                          // Idle -> Publishing
    ASSERT_EQ(fsm.get_state(), TelemetryState::Publishing);

    // When / Then: first step in Publishing sends immediately
    fsm.step();
    EXPECT_EQ(mqtt.published.size(), 1u);

    // When / Then: just before the interval ends, nothing is sent
    clock.now += PUBLISH_INTERVAL_MS - 1;
    fsm.step();
    EXPECT_EQ(mqtt.published.size(), 1u);

    // When / Then: exactly at the end of the interval, the next point is sent
    clock.now += 1;
    fsm.step();
    EXPECT_EQ(mqtt.published.size(), 2u);
}


TEST_F(TelemetryFsmTest, GoesToErrorWhenPublishFails)
{
    // Given
    enter_publishing();
    mqtt.next_result = false;

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), TelemetryState::Error);
}


TEST_F(TelemetryFsmTest, ReturnsToIdleAfterError)
{
    // Given: FSM in Error after a failed publish
    enter_publishing();
    mqtt.next_result = false;
    fsm.step();
    ASSERT_EQ(fsm.get_state(), TelemetryState::Error);

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), TelemetryState::Idle);
}


TEST_F(TelemetryFsmTest, ReturnsToIdleWhenNetworkLost)
{
    // Given
    enter_publishing();
    network.connected = false;

    // When
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), TelemetryState::Idle);
    EXPECT_TRUE(mqtt.published.empty());
}

