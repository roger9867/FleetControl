import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { environment } from '../../environments/environment';
import { Person, DrivingLicense } from '../models/person.model';

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

  // Injects the HttpClient for the API calls
  constructor(private http: HttpClient) {}

  // Loads all persons from the server and maps them to the domain model
  loadAll(): Observable<Person[]> {
    return this.http.get<PersonResponseDto[]>(`${this.baseUrl}/Person`).pipe(
      map(list => list.map(dto => this.toPerson(dto))),
      catchError(err => this.rethrow(err))
    );
  }

  // Creates a new person on the server
  save(person: Person): Observable<Person> {
    // 1. Convert the domain model into the request DTO and save it
    return this.http.post<PersonResponseDto>(`${this.baseUrl}/Person`, this.toRequestDto(person)).pipe(
      // 2. Map the response DTO back into the domain model
      map(dto => this.toPerson(dto, person.assignedVehicleId)),
      catchError(err => this.rethrow(err))
    );
  }

  // Updates an existing person on the server
  update(person: Person): Observable<Person> {
    return this.http.put<PersonResponseDto>(`${this.baseUrl}/Person/${person.Id}`, this.toRequestDto(person)).pipe(
      map(dto => this.toPerson(dto, person.assignedVehicleId)),
      catchError(err => this.rethrow(err))
    );
  }

  // Deletes a person by its id
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Person/${id}`).pipe(
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

  // Converts the Person domain model into the request DTO for the API
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

  // Converts a person response DTO into the domain model, including driver's license mapping
  private toPerson(dto: PersonResponseDto, assignedVehicleId?: string | null): Person {
    // Map driver's license classes to plain text using their index in LICENSE_TYPES
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
