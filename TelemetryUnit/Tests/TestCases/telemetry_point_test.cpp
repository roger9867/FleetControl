#include <gtest/gtest.h>


#include "fakes.hpp"
#include "../../App/Telemetry/telemetry_point.hpp"


class TelemetryPointTest : public ::testing::Test
{
    protected:
        FakeClock clock;
        FakeDeviceId device_id;

        TelemetryPoint point = TelemetryPoint(clock, device_id);
};


TEST_F(TelemetryPointTest, ParsesValidCgnssInfo)
{
    // Given
    const char* response = "+CGNSSINFO: 2,09,05,00,4807.038000,N,01131.000000,E,111026,123519.0,500.0,10.0,0.0";

    // When
    TelemetryData point_data = point.parse_gnss(response);

    // Then
    ASSERT_TRUE(point_data.valid);
    EXPECT_STREQ(point_data.date, "111026");
    EXPECT_FLOAT_EQ(point_data.accel_in_ms2, 0.0f);         // first fix: no previous speed
    EXPECT_NEAR(point_data.speed_in_kmh, 18.52, 0.01);     // 10 knots * 1.852
    EXPECT_NEAR(point_data.latitude, 48.1173, 0.0001);      // 48° + 7.038' / 60
    EXPECT_NEAR(point_data.longitude, 11.516667, 0.0001);   // 11° + 31' / 60
    EXPECT_STREQ(point_data.time, "123519.0");             
}


TEST_F(TelemetryPointTest, SouthAndWestAreNegative)
{
    // Given
    const char* response =
        "+CGNSSINFO: 2,09,05,00,4807.038000,S,01131.000000,W,111026,123519.0,500.0,10.0,0.0";

    // When
    TelemetryData point_data = point.parse_gnss(response);

    // Then
    ASSERT_TRUE(point_data.valid);
    EXPECT_NEAR(point_data.latitude, -48.1173, 0.0001);
    EXPECT_NEAR(point_data.longitude, -11.516667, 0.0001);
}
