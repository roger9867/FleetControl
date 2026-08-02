import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Vehicle } from '../models/vehicle.model';

@Injectable({ providedIn: 'root' })
export class VehicleService {

  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  loadAll(): Observable<Vehicle[]> {
    return this.http.get<Vehicle[]>(`${this.baseUrl}/Vehicle`);
  }

  save(vehicle: Vehicle): Observable<Vehicle> {
    return this.http.post<Vehicle>(`${this.baseUrl}/Vehicle`, vehicle);
  }

  update(vehicle: Vehicle): Observable<Vehicle> {
    return this.http.put<Vehicle>(`${this.baseUrl}/Vehicle/${vehicle.Id}`, vehicle);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/Vehicle/${id}`);
  }
}
