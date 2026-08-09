import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { TelemetryUnit } from '../models/telemetry-unit.model';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

const UUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

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

  filterUuidResponses(res: any): string[] {
    return Object.values(res ?? {})
      .filter((v): v is string => typeof v === 'string' && UUID_PATTERN.test(v.trim()))
      .map(v => v.trim());
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

  update(unit: TelemetryUnit): Observable<TelemetryUnit> {
    return this.http.put<TelemetryUnit>(`${this.baseUrl}/TelemetryUnit/${unit.id}`, unit).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/TelemetryUnit/${id}`).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  private rethrow(err: HttpErrorResponse) {
    const message = typeof err.error === 'string' && err.error
      ? err.error
      : `Fehler ${err.status}: ${err.statusText}`;

    return throwError(() => new Error(message));
  }
}
