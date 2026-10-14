cmake_minimum_required(VERSION 3.22)
project(TelemetryUnitTests CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Fetch GoogleTest from GitHub at configure time
include(FetchContent)
FetchContent_Declare(
    googletest
    URL https://github.com/google/googletest/archive/refs/tags/v1.17.0.zip
    DOWNLOAD_EXTRACT_TIMESTAMP TRUE
)
FetchContent_MakeAvailable(googletest)

enable_testing()

set(APP_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../App)

add_executable(telemetry_unit_tests
    TestCases/telemetry_fsm_test.cpp
    TestCases/telemetry_point_test.cpp
    TestCases/gnss_fsm_test.cpp
    TestCases/network_fsm_test.cpp
    ${APP_DIR}/Gnss/gnss_fsm.cpp
    ${APP_DIR}/Network/network_fsm.cpp
    ${APP_DIR}/Telemetry/telemetry_fsm.cpp
    ${APP_DIR}/Telemetry/telemetry_point.cpp
)
target_include_directories(telemetry_unit_tests PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/Fakes
)
target_link_libraries(telemetry_unit_tests PRIVATE GTest::gmock_main)

# Register every TEST() with CTest
include(GoogleTest)
gtest_discover_tests(telemetry_unit_tests)
