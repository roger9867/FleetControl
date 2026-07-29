#include "telemetry_fsm.hpp"


TelemetryFsm::fsm_step()
{
    switch(current_state)
    {
        case GnssState::Idle : ; break;

        case GnssState::Ready : ; break;

        case GnssState::Driving : ; break;

        case GnssState::Error : ; break;
    }
}
