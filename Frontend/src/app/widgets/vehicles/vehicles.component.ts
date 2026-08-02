import { Component, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { VehicleMap, VehicleMapPoint } from '../vehicle-map/vehicle-map.component';
import { Vehicle } from '../../models/vehicle.model';
import { Person } from '../../models/person.model';
import { VehicleService } from '../../services/vehicle.service';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [CommonModule, FormsModule, VehicleMap],
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.scss']
})
export class Vehicles implements OnInit
{
  vehicles: Vehicle[] = [];

  pageSize = 10;
  currentPage = 1;

  showCreateForm = false;
  newVehicle: Vehicle = this.emptyVehicle();

  selectedIndex: number | null = null;
  editingIndex: number | null = null;

  dummyTelemetryUnits: string[] = ['TU-1001', 'TU-1002', 'TU-1003'];

  dummyPersons: Person[] = [
    { Id: 'p1', firstName: 'Anna', lastName: 'Schmidt', employeeNr: 'MA-1000', birthDate: '1970-05-20' },
    { Id: 'p2', firstName: 'Ben', lastName: 'Müller', employeeNr: 'MA-1001', birthDate: '1971-05-20' },
    { Id: 'p3', firstName: 'Clara', lastName: 'Fischer', employeeNr: 'MA-1002', birthDate: '1972-05-20' },
    { Id: 'p4', firstName: 'David', lastName: 'Weber', employeeNr: 'MA-1003', birthDate: '1973-05-20' },
    { Id: 'p5', firstName: 'Emma', lastName: 'Meyer', employeeNr: 'MA-1004', birthDate: '1974-05-20' }
  ];

  licenseClasses: string[] = [
    'AM', 'A1', 'A2', 'A', 'B', 'BE',
    'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE'
  ];

  readonly maxVehicleFilters = 10;
  readonly maxTelemetryUnitFilters = 10;
  readonly maxPersonFilters = 10;

  filterVehicleId = '';
  filterVehicleIds: string[] = [];
  filterTelemetryUnitId = '';
  filterTelemetryUnitIds: string[] = [];
  filterPersonId = '';
  filterPersonIds: string[] = [];

  appliedFilterVehicleIds: string[] = [];
  appliedFilterTelemetryUnitIds: string[] = [];
  appliedFilterPersonIds: string[] = [];

  vehicleSearch = '';
  showVehicleOptions = false;

  telemetrySearch = '';
  showTelemetryOptions = false;

  personSearch = '';
  showPersonOptions = false;

  showAdvancedVehicleFilter = false;
  advancedVehicleFilter = this.emptyAdvancedVehicleFilter();
  appliedAdvancedVehicleFilter = this.emptyAdvancedVehicleFilter();

  showAdvancedPersonFilter = false;
  advancedPersonFilter = this.emptyAdvancedPersonFilter();
  appliedAdvancedPersonFilter = this.emptyAdvancedPersonFilter();

  @ViewChild('vehicleAutocomplete') vehicleAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild('telemetryAutocomplete') telemetryAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild('personAutocomplete') personAutocompleteRef?: ElementRef<HTMLElement>;

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node;

    if (this.vehicleAutocompleteRef && !this.vehicleAutocompleteRef.nativeElement.contains(target)) {
      this.showVehicleOptions = false;
    }

    if (this.telemetryAutocompleteRef && !this.telemetryAutocompleteRef.nativeElement.contains(target)) {
      this.showTelemetryOptions = false;
    }

    if (this.personAutocompleteRef && !this.personAutocompleteRef.nativeElement.contains(target)) {
      this.showPersonOptions = false;
    }
  }

  constructor(private vehicleService: VehicleService) {}

  ngOnInit(): void {
    this.vehicles = this.generateDummyVehicles(40);

    // Once the backend endpoint exists, a successful load replaces the dummy data.
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        if (vehicles?.length) {
          this.vehicles = vehicles;
        }
      },
      error: () => {
        // No backend yet — keep the dummy vehicles.
      }
    });
  }

  get filteredVehicles(): Vehicle[] {
    const f = this.appliedAdvancedVehicleFilter;

    return this.vehicles.filter(v => {
      if (this.appliedFilterVehicleIds.length && !this.appliedFilterVehicleIds.includes(v.licensePlate ?? '')) return false;
      if (this.appliedFilterTelemetryUnitIds.length && !this.appliedFilterTelemetryUnitIds.includes(v.telemetryUnit?.id ?? '')) return false;
      if (this.appliedFilterPersonIds.length && !this.appliedFilterPersonIds.includes(v.assignedPersonId ?? '')) return false;

      const matchesBrand = !f.brand || (v.brand ?? '').toLowerCase().includes(f.brand.toLowerCase());
      const matchesModel = !f.modelName || (v.modelName ?? '').toLowerCase().includes(f.modelName.toLowerCase());
      const matchesColor = !f.color || (v.color ?? '').toLowerCase().includes(f.color.toLowerCase());
      const matchesYearFrom = f.yearFrom == null || (v.year ?? 0) >= f.yearFrom;
      const matchesYearTo = f.yearTo == null || (v.year ?? 0) <= f.yearTo;
      const matchesIdentNr = !f.identNr || (v.identNr ?? '').toLowerCase().includes(f.identNr.toLowerCase());
      const matchesLicense = !f.requiredLicense || v.requiredLicense === f.requiredLicense;
      const matchesPowerFrom = f.powerPsFrom == null || (v.powerPs ?? 0) >= f.powerPsFrom;
      const matchesPowerTo = f.powerPsTo == null || (v.powerPs ?? 0) <= f.powerPsTo;
      const matchesRegFrom = !f.firstRegistrationFrom || (v.firstRegistration ?? '') >= f.firstRegistrationFrom;
      const matchesRegTo = !f.firstRegistrationTo || (v.firstRegistration ?? '') <= f.firstRegistrationTo;

      return matchesBrand && matchesModel && matchesColor && matchesYearFrom && matchesYearTo
        && matchesIdentNr && matchesLicense && matchesPowerFrom && matchesPowerTo && matchesRegFrom && matchesRegTo;
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredVehicles.length / this.pageSize));
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  get pagedVehicles(): Vehicle[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredVehicles.slice(start, start + this.pageSize);
  }

  get mapPoints(): VehicleMapPoint[] {
    return this.pagedVehicles
      .filter(v => v.lastLocation)
      .map(v => ({
        id: v.Id,
        label: `${v.licensePlate ?? ''} — ${v.brand ?? ''} ${v.modelName ?? ''}`,
        lat: v.lastLocation!.lat,
        lng: v.lastLocation!.lng
      }));
  }

  get filteredVehicleOptions(): Vehicle[] {
    const term = this.vehicleSearch.toLowerCase();
    const f = this.advancedVehicleFilter;

    return this.vehicles.filter(v => {
      const matchesSearch = !term || (v.licensePlate ?? '').toLowerCase().includes(term);
      const matchesBrand = !f.brand || (v.brand ?? '').toLowerCase().includes(f.brand.toLowerCase());
      const matchesModel = !f.modelName || (v.modelName ?? '').toLowerCase().includes(f.modelName.toLowerCase());
      const matchesColor = !f.color || (v.color ?? '').toLowerCase().includes(f.color.toLowerCase());
      const matchesYearFrom = f.yearFrom == null || (v.year ?? 0) >= f.yearFrom;
      const matchesYearTo = f.yearTo == null || (v.year ?? 0) <= f.yearTo;
      const matchesIdentNr = !f.identNr || (v.identNr ?? '').toLowerCase().includes(f.identNr.toLowerCase());
      const matchesLicense = !f.requiredLicense || v.requiredLicense === f.requiredLicense;
      const matchesPowerFrom = f.powerPsFrom == null || (v.powerPs ?? 0) >= f.powerPsFrom;
      const matchesPowerTo = f.powerPsTo == null || (v.powerPs ?? 0) <= f.powerPsTo;
      const matchesRegFrom = !f.firstRegistrationFrom || (v.firstRegistration ?? '') >= f.firstRegistrationFrom;
      const matchesRegTo = !f.firstRegistrationTo || (v.firstRegistration ?? '') <= f.firstRegistrationTo;

      return matchesSearch && matchesBrand && matchesModel && matchesColor
        && matchesYearFrom && matchesYearTo && matchesIdentNr && matchesLicense
        && matchesPowerFrom && matchesPowerTo && matchesRegFrom && matchesRegTo;
    });
  }

  get filteredTelemetryOptions(): string[] {
    const term = this.telemetrySearch.toLowerCase();
    return this.dummyTelemetryUnits.filter(u => u.toLowerCase().includes(term));
  }

  get filteredPersonOptions(): Person[] {
    const term = this.personSearch.toLowerCase();
    const f = this.advancedPersonFilter;

    return this.dummyPersons.filter(p => {
      const fullName = `${p.firstName ?? ''} ${p.lastName ?? ''}`.toLowerCase();
      const matchesSearch = !term || fullName.includes(term) || (p.employeeNr ?? '').toLowerCase().includes(term);
      const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
      const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
      const matchesEmployeeNr = !f.employeeNr || (p.employeeNr ?? '').toLowerCase().includes(f.employeeNr.toLowerCase());
      const matchesBirthFrom = !f.birthDateFrom || (p.birthDate ?? '') >= f.birthDateFrom;
      const matchesBirthTo = !f.birthDateTo || (p.birthDate ?? '') <= f.birthDateTo;

      return matchesSearch && matchesFirstName && matchesLastName && matchesEmployeeNr && matchesBirthFrom && matchesBirthTo;
    });
  }

  onVehicleSearchChange(): void {
    this.showVehicleOptions = true;
    if (!this.vehicleSearch) this.filterVehicleId = '';
  }

  toggleAdvancedVehicleFilter(): void {
    this.showAdvancedVehicleFilter = !this.showAdvancedVehicleFilter;
  }

  selectVehicle(vehicle: Vehicle): void {
    this.filterVehicleId = vehicle.licensePlate ?? '';
    this.vehicleSearch = vehicle.licensePlate ?? '';
    this.showVehicleOptions = false;
  }

  addVehicleFilter(): void {
    if (this.filterVehicleId
      && !this.filterVehicleIds.includes(this.filterVehicleId)
      && this.filterVehicleIds.length < this.maxVehicleFilters) {
      this.filterVehicleIds.push(this.filterVehicleId);
    }

    this.filterVehicleId = '';
    this.vehicleSearch = '';
  }

  removeVehicleFilter(vehicleId: string): void {
    this.filterVehicleIds = this.filterVehicleIds.filter(id => id !== vehicleId);
  }

  onTelemetrySearchChange(): void {
    this.showTelemetryOptions = true;
    if (!this.telemetrySearch) this.filterTelemetryUnitId = '';
  }

  selectTelemetryUnit(unitId: string): void {
    this.filterTelemetryUnitId = unitId;
    this.telemetrySearch = unitId;
    this.showTelemetryOptions = false;
  }

  addTelemetryUnitFilter(): void {
    if (this.filterTelemetryUnitId
      && !this.filterTelemetryUnitIds.includes(this.filterTelemetryUnitId)
      && this.filterTelemetryUnitIds.length < this.maxTelemetryUnitFilters) {
      this.filterTelemetryUnitIds.push(this.filterTelemetryUnitId);
    }

    this.filterTelemetryUnitId = '';
    this.telemetrySearch = '';
  }

  removeTelemetryUnitFilter(unitId: string): void {
    this.filterTelemetryUnitIds = this.filterTelemetryUnitIds.filter(id => id !== unitId);
  }

  onPersonSearchChange(): void {
    this.showPersonOptions = true;
    if (!this.personSearch) this.filterPersonId = '';
  }

  toggleAdvancedPersonFilter(): void {
    this.showAdvancedPersonFilter = !this.showAdvancedPersonFilter;
  }

  selectPerson(person: Person): void {
    this.filterPersonId = person.employeeNr ?? '';
    this.personSearch = person.employeeNr ?? '';
    this.showPersonOptions = false;
  }

  addPersonFilter(): void {
    if (this.filterPersonId
      && !this.filterPersonIds.includes(this.filterPersonId)
      && this.filterPersonIds.length < this.maxPersonFilters) {
      this.filterPersonIds.push(this.filterPersonId);
    }

    this.filterPersonId = '';
    this.personSearch = '';
  }

  removePersonFilter(personId: string): void {
    this.filterPersonIds = this.filterPersonIds.filter(id => id !== personId);
  }

  applyFilters(): void {
    this.appliedFilterVehicleIds = [...this.filterVehicleIds];
    this.appliedFilterTelemetryUnitIds = [...this.filterTelemetryUnitIds];
    this.appliedFilterPersonIds = [...this.filterPersonIds];
    this.appliedAdvancedVehicleFilter = { ...this.advancedVehicleFilter };
    this.appliedAdvancedPersonFilter = { ...this.advancedPersonFilter };
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  resetFilters(): void {
    this.filterVehicleId = '';
    this.filterVehicleIds = [];
    this.filterTelemetryUnitId = '';
    this.filterTelemetryUnitIds = [];
    this.filterPersonId = '';
    this.filterPersonIds = [];
    this.appliedFilterVehicleIds = [];
    this.appliedFilterTelemetryUnitIds = [];
    this.appliedFilterPersonIds = [];
    this.vehicleSearch = '';
    this.telemetrySearch = '';
    this.personSearch = '';
    this.showAdvancedVehicleFilter = false;
    this.showAdvancedPersonFilter = false;
    this.advancedVehicleFilter = this.emptyAdvancedVehicleFilter();
    this.appliedAdvancedVehicleFilter = this.emptyAdvancedVehicleFilter();
    this.advancedPersonFilter = this.emptyAdvancedPersonFilter();
    this.appliedAdvancedPersonFilter = this.emptyAdvancedPersonFilter();
    this.currentPage = 1;
    this.selectedIndex = null;
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

  private emptyAdvancedVehicleFilter() {
    return {
      brand: '',
      modelName: '',
      color: '',
      yearFrom: null as number | null,
      yearTo: null as number | null,
      identNr: '',
      requiredLicense: '',
      powerPsFrom: null as number | null,
      powerPsTo: null as number | null,
      firstRegistrationFrom: '',
      firstRegistrationTo: ''
    };
  }

  private emptyAdvancedPersonFilter() {
    return {
      firstName: '',
      lastName: '',
      employeeNr: '',
      birthDateFrom: '',
      birthDateTo: ''
    };
  }

  private generateDummyVehicles(count: number): Vehicle[] {
    const brands = ['VW', 'Mercedes', 'BMW', 'Audi', 'Ford', 'Opel', 'Renault', 'Toyota'];
    const models = ['Transporter', 'Sprinter', 'X3', 'A4', 'Transit', 'Astra', 'Trafic', 'Hilux'];
    const colors = ['Schwarz', 'Weiß', 'Silber', 'Blau', 'Rot', 'Grau'];

    const baseLat = 50.9271;
    const baseLng = 11.5892;

    return Array.from({ length: count }, (_, i) => {
      const year = 2010 + (i % 15);
      const offset = (i % 10) - 5;

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
        firstRegistration: `${year}-01-01`,
        assignedPersonId: i % 3 === 0 ? this.dummyPersons[i % this.dummyPersons.length].employeeNr ?? null : null,
        lastLocation: {
          lat: baseLat + offset * 0.003 + (Math.random() - 0.5) * 0.001,
          lng: baseLng + offset * 0.003 + (Math.random() - 0.5) * 0.001,
          timestamp: new Date(2026, 6, 20, 8, i % 60, 0).toISOString()
        }
      };
    });
  }
}
