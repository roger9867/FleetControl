export interface TripPoint {
  lat: number;
  lng: number;
  timestamp: string;
  speedKmh: number;
  accelMs2: number;
}

export interface Trip {
  id: string;
  vehicleId: string;
  telemetryUnitId: string;
  start: string;
  end: string;
  points: TripPoint[];
}
