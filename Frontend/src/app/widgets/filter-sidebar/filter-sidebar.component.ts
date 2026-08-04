import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { Vehicle } from '../../models/vehicle.model';
import { Person } from '../../models/person.model';

export interface VehicleAdvancedFilter {
  brand: string;
  modelName: string;
  color: string;
  yearFrom: number | null;
  yearTo: number | null;
  identNr: string;
  requiredLicense: string;
  powerPsFrom: number | null;
  powerPsTo: number | null;
  firstRegistrationFrom: string;
  firstRegistrationTo: string;
}

export interface PersonAdvancedFilter {
  firstName: string;
  lastName: string;
  employeeNr: string;
}

export type FilterMode = 'intersect' | 'union';

export interface AppliedFilters {
  vehicleIds: string[];
  telemetryUnitIds: string[];
  personIds: string[];
  vehicleAdvanced: VehicleAdvancedFilter;
  personAdvanced: PersonAdvancedFilter;
  mode: FilterMode;
}

export function emptyVehicleAdvancedFilter(): VehicleAdvancedFilter {
  return {
    brand: '',
    modelName: '',
    color: '',
    yearFrom: null,
    yearTo: null,
    identNr: '',
    requiredLicense: '',
    powerPsFrom: null,
    powerPsTo: null,
    firstRegistrationFrom: '',
    firstRegistrationTo: ''
  };
}

export function emptyPersonAdvancedFilter(): PersonAdvancedFilter {
  return {
    firstName: '',
    lastName: '',
    employeeNr: ''
  };
}

export function emptyAppliedFilters(): AppliedFilters {
  return {
    vehicleIds: [],
    telemetryUnitIds: [],
    personIds: [],
    vehicleAdvanced: emptyVehicleAdvancedFilter(),
    personAdvanced: emptyPersonAdvancedFilter(),
    mode: 'intersect'
  };
}

// Shared across the Fahrzeuge, Personen and Datenerfassungseinheiten pages —
// each page feeds in its own vehicles/telemetry/persons lists and reacts to
// filtersApplied by filtering whichever list it displays.
@Component({
  selector: 'app-filter-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './filter-sidebar.component.html',
  styleUrls: ['./filter-sidebar.component.scss']
})
export class FilterSidebar
{
  @Input() vehicles: Vehicle[] = [];
  @Input() telemetryUnitIds: string[] = [];
  @Input() persons: Person[] = [];
  @Input() licenseClasses: string[] = [];

  @Output() filtersApplied = new EventEmitter<AppliedFilters>();

  readonly maxVehicleFilters = 10;
  readonly maxTelemetryUnitFilters = 10;
  readonly maxPersonFilters = 10;

  filterVehicleId = '';
  filterVehicleIds: string[] = [];
  filterTelemetryUnitId = '';
  filterTelemetryUnitIds: string[] = [];
  filterPersonId = '';
  filterPersonIds: string[] = [];

  vehicleSearch = '';
  showVehicleOptions = false;

  telemetrySearch = '';
  showTelemetryOptions = false;

  personSearch = '';
  showPersonOptions = false;

  showAdvancedVehicleFilter = false;
  advancedVehicleFilter = emptyVehicleAdvancedFilter();

  showAdvancedPersonFilter = false;
  advancedPersonFilter = emptyPersonAdvancedFilter();

  filterMode: FilterMode = 'intersect';

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

  get filteredVehicleOptions(): Vehicle[] {
    const term = this.vehicleSearch.toLowerCase();
    const f = this.advancedVehicleFilter;

    return this.vehicles.filter(v => {
      const matchesSearch = !term || (v.licensePlate ?? '').toLowerCase().includes(term) || v.Id.toLowerCase().includes(term);
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
    return this.telemetryUnitIds.filter(u => u.toLowerCase().includes(term));
  }

  get filteredPersonOptions(): Person[] {
    const term = this.personSearch.toLowerCase();
    const f = this.advancedPersonFilter;

    return this.persons.filter(p => {
      const fullName = `${p.firstName ?? ''} ${p.lastName ?? ''}`.toLowerCase();
      const matchesSearch = !term || fullName.includes(term) || p.Id.toLowerCase().includes(term);
      const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
      const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
      const matchesEmployeeNr = !f.employeeNr || p.Id.toLowerCase().includes(f.employeeNr.toLowerCase());

      return matchesSearch && matchesFirstName && matchesLastName && matchesEmployeeNr;
    });
  }

  setFilterMode(mode: FilterMode): void {
    this.filterMode = mode;
  }

  toggleAdvancedVehicleFilter(): void {
    this.showAdvancedVehicleFilter = !this.showAdvancedVehicleFilter;
  }

  toggleAdvancedPersonFilter(): void {
    this.showAdvancedPersonFilter = !this.showAdvancedPersonFilter;
  }

  onVehicleSearchChange(): void {
    this.showVehicleOptions = true;
    if (!this.vehicleSearch) this.filterVehicleId = '';
  }

  selectVehicle(vehicle: Vehicle): void {
    this.filterVehicleId = vehicle.Id;
    this.vehicleSearch = vehicle.licensePlate ?? vehicle.Id;
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

  selectPerson(person: Person): void {
    this.filterPersonId = person.Id;
    this.personSearch = person.Id;
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

  hasActiveFilters = false;

  private computeHasActiveFilters(f: AppliedFilters): boolean {
    const va = f.vehicleAdvanced;
    const pa = f.personAdvanced;

    return f.vehicleIds.length > 0
      || f.telemetryUnitIds.length > 0
      || f.personIds.length > 0
      || !!(va.brand || va.modelName || va.color || va.identNr || va.requiredLicense
        || va.yearFrom != null || va.yearTo != null || va.powerPsFrom != null || va.powerPsTo != null
        || va.firstRegistrationFrom || va.firstRegistrationTo)
      || !!(pa.firstName || pa.lastName || pa.employeeNr);
  }

  applyFilters(): void {
    const filters: AppliedFilters = {
      vehicleIds: [...this.filterVehicleIds],
      telemetryUnitIds: [...this.filterTelemetryUnitIds],
      personIds: [...this.filterPersonIds],
      vehicleAdvanced: { ...this.advancedVehicleFilter },
      personAdvanced: { ...this.advancedPersonFilter },
      mode: this.filterMode
    };

    this.hasActiveFilters = this.computeHasActiveFilters(filters);
    this.filtersApplied.emit(filters);
  }

  resetFilters(): void {
    this.filterVehicleId = '';
    this.filterVehicleIds = [];
    this.filterTelemetryUnitId = '';
    this.filterTelemetryUnitIds = [];
    this.filterPersonId = '';
    this.filterPersonIds = [];
    this.vehicleSearch = '';
    this.telemetrySearch = '';
    this.personSearch = '';
    this.showAdvancedVehicleFilter = false;
    this.showAdvancedPersonFilter = false;
    this.advancedVehicleFilter = emptyVehicleAdvancedFilter();
    this.advancedPersonFilter = emptyPersonAdvancedFilter();
    this.filterMode = 'intersect';
    this.hasActiveFilters = false;

    this.filtersApplied.emit(emptyAppliedFilters());
  }
}
