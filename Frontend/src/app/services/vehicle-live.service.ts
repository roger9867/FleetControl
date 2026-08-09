import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../../environments/environment';

export interface VehiclePositionUpdate {
  vehicleId: string;
  telemetryUnitId: string;
  lat: number;
  lng: number;
  speedKmh: number;
  accelMs2: number;
  timestamp: string;
}

export interface TripEndedUpdate {
  tripId: string;
  vehicleId: string | null;
  endTimestamp: string;
}

@Injectable({ providedIn: 'root' })
export class VehicleLiveService {

  private connection: signalR.HubConnection;
  private vehicleUpdateSubject = new Subject<VehiclePositionUpdate>();
  private tripEndedSubject = new Subject<TripEndedUpdate>();
  private usbUnitsChangedSubject = new Subject<string[]>();

  vehicleUpdate$ = this.vehicleUpdateSubject.asObservable();
  vehicleMoving$ = this.vehicleUpdate$.pipe(map(update => update.vehicleId));
  tripEnded$ = this.tripEndedSubject.asObservable();
  usbUnitsChanged$ = this.usbUnitsChangedSubject.asObservable();

  constructor() {
    const hubUrl = `${environment.apiUrl.replace(/\/api\/?$/, '')}/hubs/vehicles`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    this.connection.on('VehicleUpdate', (update: VehiclePositionUpdate) => {
      this.vehicleUpdateSubject.next(update);
    });

    this.connection.on('TripEnded', (update: TripEndedUpdate) => {
      this.tripEndedSubject.next(update);
    });

    this.connection.on('UsbUnitsChanged', (uuids: string[]) => {
      this.usbUnitsChangedSubject.next(uuids);
    });

    this.connection.start().catch(() => {});
  }
}
