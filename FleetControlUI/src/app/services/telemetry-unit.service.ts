import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { TelemetryUnit } from '../models/telemetry-unit.model';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class TelemetryUnitService {

  // Get base string from env
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Creates a new telemetry unit on the server
  createUnit(dto: TelemetryUnit): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.baseUrl}/TelemetryUnit`,
      dto
    );
  }

  // Loads all existing telemetry units from the server
  getUnits(): Observable<TelemetryUnit[]> {
    return this.http.get<TelemetryUnit[]>(
      `${this.baseUrl}/TelemetryUnit/TelemetryUnits`);
  }

  // Updates an existing telemetry unit
  update(unit: TelemetryUnit): Observable<TelemetryUnit> {
    return this.http.put<TelemetryUnit>(`${this.baseUrl}/TelemetryUnit/${unit.id}`, unit).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  // Deletes a telemetry unit by its id
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/TelemetryUnit/${id}`).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  // Converts an HTTP error into a readable error message and rethrows it
  private rethrow(err: HttpErrorResponse) {
    const message = typeof err.error === 'string' && err.error
      ? err.error
      : `Fehler ${err.status}: ${err.statusText}`;

    return throwError(() => new Error(message));
  }
}
