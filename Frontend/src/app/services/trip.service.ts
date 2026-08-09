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

  constructor(private http: HttpClient) {}

  loadPage(page: number, pageSize: number): Observable<TripPage> {
    return this.http.get<TripPageResponseDto>(`${this.baseUrl}/Trip`, {
      params: { page, pageSize }
    }).pipe(
      map(dto => ({
        trips: dto.trips.map(t => this.toTrip(t)),
        totalCount: dto.totalCount
      })),
      catchError(err => this.rethrow(err))
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Trip/${id}`).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  private rethrow(err: HttpErrorResponse) {
    const message = typeof err.error === 'string' && err.error
      ? err.error
      : `Fehler ${err.status}: ${err.statusText}`;

    return throwError(() => new Error(message));
  }

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
