import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { environment } from '../../environments/environment';
import { Person, DrivingLicense } from '../models/person.model';

// Mirrors the backend's DriversLicenseType enum order exactly — GET responses
// serialize the enum as its numeric index, so this table maps it back to the string.
const LICENSE_TYPES = [
  'AM', 'A1', 'A2', 'A', 'B', 'B96', 'BE',
  'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE', 'L', 'T'
];

interface PersonRequestDto {
  firstName: string;
  lastName: string;
  birthDate: string;
  licenses: { licenseClass: string; obtainedDate: string }[];
  assignedVehicleId?: string | null;
}

interface PersonResponseDto {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  licenses: { id: string; licenseType: number; obtainedDate: string }[];
}

@Injectable({ providedIn: 'root' })
export class PersonService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  loadAll(): Observable<Person[]> {
    return this.http.get<PersonResponseDto[]>(`${this.baseUrl}/Person`).pipe(
      map(list => list.map(dto => this.toPerson(dto))),
      catchError(err => this.rethrow(err))
    );
  }

  save(person: Person): Observable<Person> {
    return this.http.post<PersonResponseDto>(`${this.baseUrl}/Person`, this.toRequestDto(person)).pipe(
      map(dto => this.toPerson(dto, person.assignedVehicleId)),
      catchError(err => this.rethrow(err))
    );
  }

  update(person: Person): Observable<Person> {
    return this.http.put<PersonResponseDto>(`${this.baseUrl}/Person/${person.Id}`, this.toRequestDto(person)).pipe(
      map(dto => this.toPerson(dto, person.assignedVehicleId)),
      catchError(err => this.rethrow(err))
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Person/${id}`).pipe(
      catchError(err => this.rethrow(err))
    );
  }

  private rethrow(err: HttpErrorResponse) {
    const message = typeof err.error === 'string' && err.error
      ? err.error
      : `Fehler ${err.status}: ${err.statusText}`;

    return throwError(() => new Error(message));
  }

  private toRequestDto(person: Person): PersonRequestDto {
    return {
      firstName: person.firstName ?? '',
      lastName: person.lastName ?? '',
      birthDate: person.birthDate ?? '',
      licenses: (person.licenses ?? []).map(l => ({
        licenseClass: l.licenseClass,
        obtainedDate: l.obtainedDate
      })),
      assignedVehicleId: person.assignedVehicleId || null
    };
  }

  // The backend's VehicleDriver has no inverse "assigned vehicle" field (the FK
  // lives on Vehicle), so it can't be read back from the response — the caller
  // passes through what it just submitted for save()/update().
  private toPerson(dto: PersonResponseDto, assignedVehicleId?: string | null): Person {
    const licenses: DrivingLicense[] = (dto.licenses ?? []).map(l => ({
      licenseClass: LICENSE_TYPES[l.licenseType] ?? '',
      obtainedDate: l.obtainedDate
    }));

    return {
      Id: dto.id,
      firstName: dto.firstName,
      lastName: dto.lastName,
      birthDate: dto.dateOfBirth,
      licenses,
      assignedVehicleId: assignedVehicleId ?? null
    };
  }
}
