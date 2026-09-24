export interface TripPoint {
  lat: number;
  lng: number;
  timestamp: string;
  speedKmh: number;
  accelMs2: number;
}

export interface Trip {
  id: string;
  vehicleId?: string | null;
  telemetryUnitId: string;
  driverId?: string | null;
  start: string;
  end?: string | null;
  points: TripPoint[];
}
