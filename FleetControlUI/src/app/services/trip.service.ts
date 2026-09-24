import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { environment } from '../../environments/environment';
import { Trip, TripPoint } from '../models/trip.model';

interface TripPointResponseDto {
  timestamp: string;
  lat: number;
  lng: number;
  speedKmh: number;
  accelMs2: number;
}

interface TripResponseDto {
  id: string;
  vehicleId?: string | null;
  telemetryUnitId: string;
  driverId?: string | null;
  start: string;
  end?: string | null;
  points: TripPointResponseDto[];
}

interface TripPageResponseDto {
  trips: TripResponseDto[];
  totalCount: number;
}

export interface TripPage {
  trips: Trip[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class TripService {

  private baseUrl = environment.apiUrl;

  // Injiziert den HttpClient für die API-Aufrufe
  constructor(private http: HttpClient) {}

  // Lädt eine Seite von Fahrten vom Server und mappt sie auf das Domain-Modell
  loadPage(page: number, pageSize: number): Observable<TripPage> {
    // 1. Seite von Fahrten vom Server laden
    return this.http.get<TripPageResponseDto>(`${this.baseUrl}/Trip`, {
      params: { page, pageSize }
    }).pipe(
      // 2. DTOs auf das Domain-Modell mappen
      map(dto => ({
        trips: dto.trips.map(t => this.toTrip(t)),
        totalCount: dto.totalCount
      })),
      catchError(err => this.rethrow(err))
    );
  }

  // Löscht eine Fahrt anhand ihrer Id
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Trip/${id}`).pipe(
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

  // Wandelt ein Trip-Response-DTO in das Domain-Modell Trip um
  private toTrip(dto: TripResponseDto): Trip {
    return {
      id: dto.id,
      vehicleId: dto.vehicleId ?? null,
      telemetryUnitId: dto.telemetryUnitId,
      driverId: dto.driverId ?? null,
      start: dto.start,
      end: dto.end ?? null,
      points: dto.points.map((p): TripPoint => ({
        lat: p.lat,
        lng: p.lng,
        timestamp: p.timestamp,
        speedKmh: p.speedKmh,
        accelMs2: p.accelMs2
      }))
    };
  }
}
