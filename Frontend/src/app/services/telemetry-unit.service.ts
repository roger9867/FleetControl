import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { TelemetryUnit } from '../models/telemetry-unit.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TelemetryUnitService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  broadcastCommand(): Observable<any> {
    return this.http.post(
      `${this.baseUrl}/TelemetryUnit/broadcast`,
      JSON.stringify('get_device_id'),
      {
        headers: { 'Content-Type': 'application/json' }
      }
    );
  }

  createUnit(dto: TelemetryUnit): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      ` ${this.baseUrl}/TelemetryUnit`,
      dto
    );
  }

  getUnits(): Observable<TelemetryUnit[]> {
    return this.http.get<TelemetryUnit[]>(
      `${this.baseUrl}/TelemetryUnit/TelemetryUnits`);
  }
}
