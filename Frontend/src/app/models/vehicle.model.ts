import { TelemetryUnit } from './telemetry-unit.model';

export interface Vehicle {
  Id: string;
  modelName?: string;
  brand?: string;
  licensePlate?: string;
  year?: number;

  identNr?: string;
  requiredLicense?: string;
  powerPs?: number;
  color?: string;
  firstRegistration?: string;

  telemetryUnit?: TelemetryUnit | null;
  assignedPersonId?: string | null;

  lastLocation?: {
    lat: number;
    lng: number;
    timestamp: string;
    telemetryUnitId?: string;
    speedKmh?: number;
    accelMs2?: number;
  };
}