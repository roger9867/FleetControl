import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { environment } from '../../environments/environment';
import { Vehicle } from '../models/vehicle.model';

// Mirrors the backend's DriversLicenseType enum order exactly — GET responses
// serialize the enum as its numeric index, so this table maps it back to the string.
const LICENSE_TYPES = [
  'AM', 'A1', 'A2', 'A', 'B', 'B96', 'BE',
  'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE', 'L', 'T'
];

interface VehicleRequestDto {
  modelName: string;
  licensePlate?: string | null;
  identNr: string;
  brand?: string | null;
  year?: number | null;
  requiredLicense?: string | null;
  powerPs?: number | null;
  color?: string | null;
  firstRegistration?: string | null;
  assignedPersonId?: string | null;
  telemetryUnitId?: string | null;
}

interface VehicleResponseDto {
  id: string;
  modelName: string;
  identificationNumber: string;
  licensePlateNumber?: string | null;
  brand?: string | null;
  year?: number | null;
  requiredLicense?: number | null;
  powerPs?: number | null;
  color?: string | null;
  firstRegistration?: string | null;
  vehicleDriverId?: string | null;
  telemetryUnit?: { id: string } | null;
}

@Injectable({ providedIn: 'root' })
export class VehicleService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  loadAll(): Observable<Vehicle[]> {
    return this.http.get<VehicleResponseDto[]>(`${this.baseUrl}/Vehicle`).pipe(
      map(list => list.map(dto => this.toVehicle(dto))),
      catchError(err => this.rethrow(err))
    );
  }

  save(vehicle: Vehicle): Observable<Vehicle> {
    return this.http.post<VehicleResponseDto>(`${this.baseUrl}/Vehicle`, this.toRequestDto(vehicle)).pipe(
      map(dto => this.toVehicle(dto)),
      catchError(err => this.rethrow(err))
    );
  }

  update(vehicle: Vehicle): Observable<Vehicle> {
    return this.http.put<VehicleResponseDto>(`${this.baseUrl}/Vehicle/${encodeURIComponent(vehicle.Id)}`, this.toRequestDto(vehicle)).pipe(
      map(dto => this.toVehicle(dto)),
      catchError(err => this.rethrow(err))
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Vehicle/${encodeURIComponent(id)}`).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  private rethrow(err: HttpErrorResponse) {
    const message = typeof err.error === 'string' && err.error
      ? err.error
      : `Fehler ${err.status}: ${err.statusText}`;

    return throwError(() => new Error(message));
  }

  private toRequestDto(vehicle: Vehicle): VehicleRequestDto {
    return {
      modelName: vehicle.modelName ?? '',
      licensePlate: vehicle.licensePlate || null,
      identNr: vehicle.identNr ?? '',
      brand: vehicle.brand || null,
      year: vehicle.year ?? null,
      requiredLicense: vehicle.requiredLicense || null,
      powerPs: vehicle.powerPs ?? null,
      color: vehicle.color || null,
      firstRegistration: vehicle.firstRegistration || null,
      assignedPersonId: vehicle.assignedPersonId || null,
      telemetryUnitId: vehicle.telemetryUnit?.id || null
    };
  }

  private toVehicle(dto: VehicleResponseDto): Vehicle {
    return {
      Id: dto.id,
      modelName: dto.modelName,
      identNr: dto.identificationNumber,
      licensePlate: dto.licensePlateNumber ?? undefined,
      brand: dto.brand ?? undefined,
      year: dto.year ?? undefined,
      requiredLicense: dto.requiredLicense != null ? LICENSE_TYPES[dto.requiredLicense] : undefined,
      powerPs: dto.powerPs ?? undefined,
      color: dto.color ?? undefined,
      firstRegistration: dto.firstRegistration ?? undefined,
      assignedPersonId: dto.vehicleDriverId ?? null,
      telemetryUnit: dto.telemetryUnit ? { id: dto.telemetryUnit.id } : null
    };
  }
}
