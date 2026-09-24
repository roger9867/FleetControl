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

// Erzeugt ein leeres erweitertes Filterobjekt für Fahrzeuge
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

// Erzeugt ein leeres erweitertes Filterobjekt für Personen
export function emptyPersonAdvancedFilter(): PersonAdvancedFilter {
  return {
    firstName: '',
    lastName: '',
    employeeNr: ''
  };
}

// Erzeugt ein leeres Gesamt-Filterobjekt mit Standardwerten
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

@Component({
  selector: 'app-filter-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './filter-sidebar.component.html',
  styleUrls: ['./filter-sidebar.component.scss']
})
export class FilterComponent
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

  // Schliesst offene Autocomplete-Dropdowns bei Klick ausserhalb ihres Bereichs
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

  // Liefert die Fahrzeuge, die Suchbegriff und erweiterte Filterkriterien erfüllen
  get filteredVehicleOptions(): Vehicle[] {
    const term = this.vehicleSearch.toLowerCase();
    const f = this.advancedVehicleFilter;

    return this.vehicles.filter(v => {
      // Einzelne Filterkriterien prüfen
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

      // Alle Kriterien müssen gleichzeitig erfüllt sein
      return matchesSearch && matchesBrand && matchesModel && matchesColor
        && matchesYearFrom && matchesYearTo && matchesIdentNr && matchesLicense
        && matchesPowerFrom && matchesPowerTo && matchesRegFrom && matchesRegTo;
    });
  }

  // Liefert die Telemetrie-Einheiten-IDs, die zum Suchbegriff passen
  get filteredTelemetryOptions(): string[] {
    const term = this.telemetrySearch.toLowerCase();
    return this.telemetryUnitIds.filter(u => u.toLowerCase().includes(term));
  }

  // Liefert die Personen, die Suchbegriff und erweiterte Filterkriterien erfüllen
  get filteredPersonOptions(): Person[] {
    const term = this.personSearch.toLowerCase();
    const f = this.advancedPersonFilter;

    return this.persons.filter(p => {
      // Einzelne Filterkriterien prüfen
      const fullName = `${p.firstName ?? ''} ${p.lastName ?? ''}`.toLowerCase();
      const matchesSearch = !term || fullName.includes(term) || p.Id.toLowerCase().includes(term);
      const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
      const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
      const matchesEmployeeNr = !f.employeeNr || p.Id.toLowerCase().includes(f.employeeNr.toLowerCase());

      // Alle Kriterien müssen gleichzeitig erfüllt sein
      return matchesSearch && matchesFirstName && matchesLastName && matchesEmployeeNr;
    });
  }

  // Setzt den Verknüpfungsmodus (Schnittmenge/Vereinigung) für Filter
  setFilterMode(mode: FilterMode): void {
    this.filterMode = mode;
  }

  // Blendet den erweiterten Fahrzeugfilter ein oder aus
  toggleAdvancedVehicleFilter(): void {
    this.showAdvancedVehicleFilter = !this.showAdvancedVehicleFilter;
  }

  // Blendet den erweiterten Personenfilter ein oder aus
  toggleAdvancedPersonFilter(): void {
    this.showAdvancedPersonFilter = !this.showAdvancedPersonFilter;
  }

  // Reagiert auf Änderungen im Fahrzeug-Suchfeld und öffnet die Vorschlagsliste
  onVehicleSearchChange(): void {
    this.showVehicleOptions = true;
    if (!this.vehicleSearch) this.filterVehicleId = '';
  }

  // Übernimmt ein ausgewähltes Fahrzeug aus der Autocomplete-Liste
  selectVehicle(vehicle: Vehicle): void {
    this.filterVehicleId = vehicle.Id;
    this.vehicleSearch = vehicle.licensePlate ?? vehicle.Id;
    this.showVehicleOptions = false;
  }

  // Fügt das ausgewählte Fahrzeug der Filterliste hinzu, sofern gültig und nicht doppelt
  addVehicleFilter(): void {
    if (this.filterVehicleId
      && !this.filterVehicleIds.includes(this.filterVehicleId)
      && this.filterVehicleIds.length < this.maxVehicleFilters) {
      this.filterVehicleIds.push(this.filterVehicleId);
    }

    // Eingabefelder für die nächste Auswahl zurücksetzen
    this.filterVehicleId = '';
    this.vehicleSearch = '';
  }

  // Entfernt ein Fahrzeug aus der Filterliste
  removeVehicleFilter(vehicleId: string): void {
    this.filterVehicleIds = this.filterVehicleIds.filter(id => id !== vehicleId);
  }

  // Reagiert auf Änderungen im Telemetrie-Suchfeld und öffnet die Vorschlagsliste
  onTelemetrySearchChange(): void {
    this.showTelemetryOptions = true;
    if (!this.telemetrySearch) this.filterTelemetryUnitId = '';
  }

  // Übernimmt eine ausgewählte Telemetrie-Einheit aus der Autocomplete-Liste
  selectTelemetryUnit(unitId: string): void {
    this.filterTelemetryUnitId = unitId;
    this.telemetrySearch = unitId;
    this.showTelemetryOptions = false;
  }

  // Fügt die ausgewählte Telemetrie-Einheit der Filterliste hinzu, sofern gültig und nicht doppelt
  addTelemetryUnitFilter(): void {
    if (this.filterTelemetryUnitId
      && !this.filterTelemetryUnitIds.includes(this.filterTelemetryUnitId)
      && this.filterTelemetryUnitIds.length < this.maxTelemetryUnitFilters) {
      this.filterTelemetryUnitIds.push(this.filterTelemetryUnitId);
    }

    // Eingabefelder für die nächste Auswahl zurücksetzen
    this.filterTelemetryUnitId = '';
    this.telemetrySearch = '';
  }

  // Entfernt eine Telemetrie-Einheit aus der Filterliste
  removeTelemetryUnitFilter(unitId: string): void {
    this.filterTelemetryUnitIds = this.filterTelemetryUnitIds.filter(id => id !== unitId);
  }

  // Reagiert auf Änderungen im Personen-Suchfeld und öffnet die Vorschlagsliste
  onPersonSearchChange(): void {
    this.showPersonOptions = true;
    if (!this.personSearch) this.filterPersonId = '';
  }

  // Übernimmt eine ausgewählte Person aus der Autocomplete-Liste
  selectPerson(person: Person): void {
    this.filterPersonId = person.Id;
    this.personSearch = person.Id;
    this.showPersonOptions = false;
  }

  // Fügt die ausgewählte Person der Filterliste hinzu, sofern gültig und nicht doppelt
  addPersonFilter(): void {
    if (this.filterPersonId
      && !this.filterPersonIds.includes(this.filterPersonId)
      && this.filterPersonIds.length < this.maxPersonFilters) {
      this.filterPersonIds.push(this.filterPersonId);
    }

    // Eingabefelder für die nächste Auswahl zurücksetzen
    this.filterPersonId = '';
    this.personSearch = '';
  }

  // Entfernt eine Person aus der Filterliste
  removePersonFilter(personId: string): void {
    this.filterPersonIds = this.filterPersonIds.filter(id => id !== personId);
  }

  hasActiveFilters = false;

  // Prüft, ob irgendein Filterkriterium der übergebenen Filter aktiv gesetzt ist
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

  // Baut das aktuelle Filterobjekt zusammen und meldet es an die übergeordnete Komponente
  applyFilters(): void {
    // 1. Filterobjekt aus aktuellem Auswahlzustand zusammenstellen
    const filters: AppliedFilters = {
      vehicleIds: [...this.filterVehicleIds],
      telemetryUnitIds: [...this.filterTelemetryUnitIds],
      personIds: [...this.filterPersonIds],
      vehicleAdvanced: { ...this.advancedVehicleFilter },
      personAdvanced: { ...this.advancedPersonFilter },
      mode: this.filterMode
    };

    // 2. Aktiv-Status ermitteln und Filter-Event auslösen
    this.hasActiveFilters = this.computeHasActiveFilters(filters);
    this.filtersApplied.emit(filters);
  }

  // Setzt alle Filterauswahlen und -eingaben auf den Ausgangszustand zurück
  resetFilters(): void {
    // 1. Alle Auswahl- und Sucheingaben leeren
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

    // 2. Leere Filter an die übergeordnete Komponente melden
    this.filtersApplied.emit(emptyAppliedFilters());
  }
}
