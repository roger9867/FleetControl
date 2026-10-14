#include <gtest/gtest.h>

#include "fakes.hpp"
#include "../../App/Gnss/gnss_fsm.hpp"


// Fixture: every TEST_F gets a fresh GNSS FSM with fakes injected for all dependencies
class GnssFsmTest : public ::testing::Test
{
    protected:
        FakeGnssModem modem;
        FakeClock clock;
        FakeLogger logger;

        GnssFSM fsm{modem, clock, logger};

        // Brings the FSM from Idle via Enabling into Ready
        void enter_ready()
        {
            fsm.step();     // Idle -> Enabling
            fsm.step();     // Enabling -> Ready
            ASSERT_EQ(fsm.get_state(), GnssState::GnssReady);
        }
};


TEST_F(GnssFsmTest, StaysReadyWithoutFixBefore60s)
{
    // Given: FSM in Ready, no fix, clock at 0
    ASSERT_NO_FATAL_FAILURE(enter_ready());
    modem.fix = false;
    clock.now = 0;
    fsm.step();                     // starts the no-fix timer

    // When: just before the 60 s timeout
    clock.now = 59999;
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), GnssState::GnssReady);
}


TEST_F(GnssFsmTest, GoesToErrorWithoutFixAfter60s)
{
    // Given: FSM in Ready, no fix, clock at 0
    ASSERT_NO_FATAL_FAILURE(enter_ready());
    modem.fix = false;
    clock.now = 0;
    fsm.step();                     // starts the no-fix timer

    // When: exactly at the 60 s timeout
    clock.now = 60000;
    fsm.step();

    // Then
    EXPECT_EQ(fsm.get_state(), GnssState::GnssError);
    EXPECT_FALSE(fsm.has_valid_fix());
}
