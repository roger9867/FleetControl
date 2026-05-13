import { TelemetryUnit } from './telemetry-unit.model';

export interface Vehicle {
  Id: string;
  modelName?: string;

  telemetryUnit?: TelemetryUnit | null;
}