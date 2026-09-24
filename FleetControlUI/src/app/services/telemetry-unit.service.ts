import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { TelemetryUnit } from '../models/telemetry-unit.model';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class TelemetryUnitService {

  // Basis-String aus Env holen
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Legt eine neue Telemetrie-Einheit auf dem Server an
  createUnit(dto: TelemetryUnit): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.baseUrl}/TelemetryUnit`,
      dto
    );
  }

  // Lädt alle vorhandenen Telemetrie-Einheiten vom Server
  getUnits(): Observable<TelemetryUnit[]> {
    return this.http.get<TelemetryUnit[]>(
      `${this.baseUrl}/TelemetryUnit/TelemetryUnits`);
  }

  // Aktualisiert eine bestehende Telemetrie-Einheit
  update(unit: TelemetryUnit): Observable<TelemetryUnit> {
    return this.http.put<TelemetryUnit>(`${this.baseUrl}/TelemetryUnit/${unit.id}`, unit).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  // Löscht eine Telemetrie-Einheit anhand ihrer Id
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/TelemetryUnit/${id}`).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  // Wandelt einen HTTP-Fehler in eine lesbare Fehlermeldung um und wirft ihn erneut
  private rethrow(err: HttpErrorResponse) {
    const message = typeof err.error === 'string' && err.error
      ? err.error
      : `Fehler ${err.status}: ${err.statusText}`;

    return throwError(() => new Error(message));
  }
}
