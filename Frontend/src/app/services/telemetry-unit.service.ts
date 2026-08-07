import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { TelemetryUnit } from '../models/telemetry-unit.model';
import { Observable, throwError, interval, of } from 'rxjs';
import { catchError, map, startWith, exhaustMap } from 'rxjs/operators';

// Only responses shaped like a real device UUID (e.g. "066EFF30-334B-...")
// count as a connected unit — a serial port that answers with noise, an
// error string, or an empty line must not show up as "connected".
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

  // Extracts only the UUID-shaped values from a { port: response } broadcast
  // reply. Non-UUID responses (nulls, timeouts, garbage) are dropped.
  filterUuidResponses(res: any): string[] {
    return Object.values(res ?? {})
      .filter((v): v is string => typeof v === 'string' && UUID_PATTERN.test(v.trim()))
      .map(v => v.trim());
  }

  // Attempts a new broadcast every 500ms. Uses exhaustMap (not switchMap):
  // a single serial port can take up to its 4s read timeout to answer, so
  // cancelling-and-restarting on every tick would abort every request
  // before it ever completed — exhaustMap instead ignores ticks that land
  // while a broadcast is still in flight, and fires the next one as soon
  // as it's free.
  pollConnectedUnits(intervalMs = 500): Observable<string[]> {
    return interval(intervalMs).pipe(
      startWith(0),
      exhaustMap(() =>
        this.broadcastCommand().pipe(
          map(res => this.filterUuidResponses(res)),
          catchError(() => of([] as string[]))
        )
      )
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
