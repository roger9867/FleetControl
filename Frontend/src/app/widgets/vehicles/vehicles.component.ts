import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { Vehicle } from '../../models/vehicle.model';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.scss']
})
export class Vehicles implements OnInit
{
  vehicles: Vehicle[] = [];

  pageSize = 20;
  currentPage = 1;

  showCreateForm = false;
  newVehicle: Vehicle = this.emptyVehicle();

  selectedIndex: number | null = null;
  editingIndex: number | null = null;

  dummyTelemetryUnits: string[] = ['TU-1001', 'TU-1002', 'TU-1003'];

  licenseClasses: string[] = [
    'AM', 'A1', 'A2', 'A', 'B', 'BE',
    'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE'
  ];

  ngOnInit(): void {
    this.vehicles = this.generateDummyVehicles(40);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.vehicles.length / this.pageSize));
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  get pagedVehicles(): Vehicle[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.vehicles.slice(start, start + this.pageSize);
  }

  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
  }

  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  toggleEdit(index: number): void {
    this.editingIndex = this.editingIndex === index ? null : index;
  }

  onTelemetryChange(vehicle: Vehicle, value: string): void {
    vehicle.telemetryUnit = value ? { id: value } : null;
  }

  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) {
      this.newVehicle = this.emptyVehicle();
    }
  }

  createVehicle(): void {
    if (!this.newVehicle.licensePlate) return;

    this.vehicles.unshift({
      Id: crypto.randomUUID(),
      licensePlate: this.newVehicle.licensePlate,
      brand: this.newVehicle.brand,
      modelName: this.newVehicle.modelName,
      year: this.newVehicle.year,
      identNr: this.newVehicle.identNr,
      requiredLicense: this.newVehicle.requiredLicense,
      powerPs: this.newVehicle.powerPs,
      color: this.newVehicle.color,
      firstRegistration: this.newVehicle.firstRegistration
    });

    this.newVehicle = this.emptyVehicle();
    this.showCreateForm = false;
    this.currentPage = 1;
  }

  private emptyVehicle(): Vehicle {
    return {
      Id: '',
      modelName: '',
      brand: '',
      licensePlate: '',
      year: undefined,
      identNr: '',
      requiredLicense: '',
      powerPs: undefined,
      color: '',
      firstRegistration: ''
    };
  }

  private generateDummyVehicles(count: number): Vehicle[] {
    const brands = ['VW', 'Mercedes', 'BMW', 'Audi', 'Ford', 'Opel', 'Renault', 'Toyota'];
    const models = ['Transporter', 'Sprinter', 'X3', 'A4', 'Transit', 'Astra', 'Trafic', 'Hilux'];
    const colors = ['Schwarz', 'Weiß', 'Silber', 'Blau', 'Rot', 'Grau'];

    return Array.from({ length: count }, (_, i) => {
      const year = 2010 + (i % 15);

      return {
        Id: `veh-${i + 1}`,
        brand: brands[i % brands.length],
        modelName: models[i % models.length],
        licensePlate: `FL-${1000 + i}`,
        year,
        identNr: `${100000 + i}`,
        requiredLicense: this.licenseClasses[i % this.licenseClasses.length],
        powerPs: 90 + ((i * 15) % 300),
        color: colors[i % colors.length],
        firstRegistration: `01.01.${year}`
      };
    });
  }
}
