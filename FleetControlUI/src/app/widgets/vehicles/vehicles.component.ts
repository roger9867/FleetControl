import { Component, OnInit, OnDestroy, ChangeDetectorRef, HostListener, ViewChild, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { VehicleMapComponent, VehicleMapPoint } from '../vehicle-map/vehicle-map.component';
import { FilterComponent, AppliedFilters, emptyAppliedFilters } from '../filter-sidebar/filter-sidebar.component';
import { Vehicle } from '../../models/vehicle.model';
import { Person } from '../../models/person.model';
import { TelemetryUnit } from '../../models/telemetry-unit.model';
import { VehicleService } from '../../services/vehicle.service';
import { TelemetryUnitService } from '../../services/telemetry-unit.service';
import { PersonService } from '../../services/person.service';
import { VehicleLiveService, VehiclePositionUpdate } from '../../services/vehicle-live.service';

const MOVING_TIMEOUT_MS = 5000;

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [CommonModule, FormsModule, VehicleMapComponent, FilterComponent],
  templateUrl: './vehicles.component.html',
  styleUrls: ['./vehicles.component.scss']
})
export class VehiclesPageComponent implements OnInit, OnDestroy
{
  @ViewChild(VehicleMapComponent) vehicleMap?: VehicleMapComponent;

  @Output() showTrips = new EventEmitter<string>();

  vehicles: Vehicle[] = [];

  pageSize = 10;
  currentPage = 1;

  showCreateForm = false;
  newVehicle: Vehicle = this.emptyVehicle();

  selectedIndex: number | null = null;
  editingIndex: number | null = null;
  deleteConfirmIndex: number | null = null;

  createError: string | null = null;
  editError: string | null = null;
  editSnapshot: Vehicle | null = null;

  identNrError: string | null = null;
  yearError: string | null = null;

  telemetryUnits: TelemetryUnit[] = [];

  persons: Person[] = [];

  licenseClasses: string[] = [
    'AM', 'A1', 'A2', 'A', 'B', 'BE',
    'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE'
  ];

  appliedFilters: AppliedFilters = emptyAppliedFilters();

  openTelemetryDropdownIndex: number | null = null;

  openDriverDropdownIndex: number | null = null;
  showAdvancedDriverFilter = false;
  advancedDriverFilter = { firstName: '', lastName: '', employeeNr: '' };

  private movingVehicleIds = new Set<string>();
  private movingTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private liveSubscription?: Subscription;

  // Schließt offene Dropdowns/Löschbestätigung, wenn außerhalb geklickt wird
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node;

    // Löschbestätigung schließen, falls Klick nicht auf den Löschen-Button war
    if (this.deleteConfirmIndex !== null) {
      const targetEl = target as HTMLElement;
      if (!targetEl.closest || !targetEl.closest('.delete-btn')) {
        this.deleteConfirmIndex = null;
      }
    }

    // Telemetrie-Dropdown schließen, falls Klick außerhalb davon war
    if (this.openTelemetryDropdownIndex !== null) {
      const targetEl = target as HTMLElement;
      if (!targetEl.closest || !targetEl.closest('.unit-select')) {
        this.openTelemetryDropdownIndex = null;
      }
    }

    // Fahrer-Dropdown schließen, falls Klick außerhalb davon war
    if (this.openDriverDropdownIndex !== null) {
      const targetEl = target as HTMLElement;
      if (!targetEl.closest || !targetEl.closest('.driver-select')) {
        this.openDriverDropdownIndex = null;
      }
    }
  }

  constructor(
    private vehicleService: VehicleService,
    private telemetryUnitService: TelemetryUnitService,
    private personService: PersonService,
    private vehicleLiveService: VehicleLiveService,
    private cdr: ChangeDetectorRef
  ) {}

  // Lädt beim Start alle Fahrzeuge, Telemetrieeinheiten und Personen und abonniert Live-Updates
  ngOnInit(): void {
    // 1. Fahrzeuge laden
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        if (vehicles?.length) {
          this.vehicles = vehicles;
        }
        this.cdr.detectChanges();
        this.loadPositions();
      },
      error: () => {}
    });

    // 2. Telemetrieeinheiten laden
    this.telemetryUnitService.getUnits().subscribe({
      next: (units) => {
        if (units?.length) {
          this.telemetryUnits = units;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // 3. Personen laden
    this.personService.loadAll().subscribe({
      next: (persons) => {
        if (persons?.length) {
          this.persons = persons;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // 4. Live-Positions-Updates abonnieren
    this.liveSubscription = this.vehicleLiveService.vehicleUpdate$.subscribe(update => {
      this.onVehicleUpdate(update);
    });
  }

  // Beendet die Live-Subscription und räumt laufende Timer auf
  ngOnDestroy(): void {
    this.liveSubscription?.unsubscribe();
    this.movingTimers.forEach(timer => clearTimeout(timer));
  }

  // Verarbeitet ein Live-Positions-Update und markiert das Fahrzeug als "in Bewegung"
  private onVehicleUpdate(update: VehiclePositionUpdate): void {
    const vehicle = this.vehicles.find(v => v.Id === update.vehicleId);
    if (vehicle) {
      vehicle.lastLocation = {
        lat: update.lat,
        lng: update.lng,
        timestamp: update.timestamp,
        telemetryUnitId: update.telemetryUnitId,
        driverId: update.driverId,
        speedKmh: update.speedKmh,
        accelMs2: update.accelMs2
      };
    }

    // Fahrzeug als bewegend markieren
    this.movingVehicleIds.add(update.vehicleId);

    // Bestehenden Timeout für dieses Fahrzeug zurücksetzen
    const existingTimer = this.movingTimers.get(update.vehicleId);
    if (existingTimer) {
      clearTimeout(existingTimer);
    }

    // Nach Ablauf der Timeout-Zeit die Bewegung als beendet markieren
    this.movingTimers.set(update.vehicleId, setTimeout(() => {
      this.movingVehicleIds.delete(update.vehicleId);
      this.movingTimers.delete(update.vehicleId);
      this.cdr.detectChanges();
    }, MOVING_TIMEOUT_MS));

    this.cdr.detectChanges();
  }

  // Lädt die aktuellen Positionen aller Fahrzeuge nach
  private loadPositions(): void {
    this.vehicleService.loadPositions().subscribe({
      next: (positions) => {
        this.vehicles.forEach(v => {
          const position = positions.get(v.Id);
          if (position) {
            v.lastLocation = position;
          }
        });
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Liefert die IDs aller bekannten Telemetrieeinheiten
  get telemetryUnitIds(): string[] {
    return this.telemetryUnits.map(u => u.id);
  }

  // Prüft, ob eine Telemetrieeinheit bereits einem anderen Fahrzeug zugeordnet ist
  isUnitTakenByOtherVehicle(unit: TelemetryUnit, vehicle: Vehicle): boolean {
    return !!unit.vehicleId && unit.vehicleId !== vehicle.Id;
  }

  // Öffnet oder schließt das Telemetrie-Dropdown für die angegebene Zeile
  toggleTelemetryDropdown(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.openTelemetryDropdownIndex = this.openTelemetryDropdownIndex === index ? null : index;
  }

  // Wählt eine Telemetrieeinheit für das Fahrzeug aus und schließt das Dropdown
  selectTelemetryUnit(vehicle: Vehicle, unitId: string, disabled = false): void {
    if (disabled) return;

    this.onTelemetryChange(vehicle, unitId);
    this.openTelemetryDropdownIndex = null;
  }

  // Lädt die Telemetrieeinheiten erneut vom Server nach
  private refreshTelemetryUnits(): void {
    this.telemetryUnitService.getUnits().subscribe({
      next: (units) => {
        if (units) {
          this.telemetryUnits = units;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Filtert die Personenliste anhand der erweiterten Fahrer-Filterkriterien
  get filteredDriverOptions(): Person[] {
    const f = this.advancedDriverFilter;

    return this.persons.filter(p => {
      const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
      const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
      const matchesEmployeeNr = !f.employeeNr || p.Id.toLowerCase().includes(f.employeeNr.toLowerCase());

      return matchesFirstName && matchesLastName && matchesEmployeeNr;
    });
  }

  // Erstellt das Tooltip-Label (Name + ID) für einen Fahrer
  private getDriverTooltipLabel(driverId?: string | null): string | undefined {
    if (!driverId) return undefined;

    const person = this.persons.find(p => p.Id === driverId);
    const name = person ? `${person.firstName ?? ''} ${person.lastName ?? ''}`.trim() : '';

    return name
      ? `${name} - ${driverId}`
      : driverId;
  }

  // Prüft, ob eine Person bereits einem anderen Fahrzeug als Fahrer zugeordnet ist
  isPersonTakenByOtherVehicle(person: Person, vehicle: Vehicle): boolean {
    return this.vehicles.some(v => v.Id !== vehicle.Id && v.assignedPersonId === person.Id);
  }

  // Liefert den Anzeigenamen des dem Fahrzeug zugeordneten Fahrers
  getDriverLabel(vehicle: Vehicle): string {
    if (!vehicle.assignedPersonId) return 'keinen';

    const person = this.persons.find(p => p.Id === vehicle.assignedPersonId);
    if (!person) return vehicle.assignedPersonId;

    return `${person.firstName ?? ''} ${person.lastName ?? ''}`.trim() || vehicle.assignedPersonId;
  }

  // Öffnet oder schließt das Fahrer-Dropdown für die angegebene Zeile
  toggleDriverDropdown(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.openDriverDropdownIndex = this.openDriverDropdownIndex === index ? null : index;
  }

  // Blendet die erweiterte Fahrer-Filtermaske ein oder aus
  toggleAdvancedDriverFilter(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.showAdvancedDriverFilter = !this.showAdvancedDriverFilter;
    if (this.showAdvancedDriverFilter) {
      this.openDriverDropdownIndex = index;
    }
  }

  // Weist dem Fahrzeug den gewählten Fahrer zu und schliesst das Dropdown
  selectDriver(vehicle: Vehicle, personId: string, disabled = false): void {
    if (disabled) return;

    vehicle.assignedPersonId = personId || null;
    this.openDriverDropdownIndex = null;
  }

  // Prüft, ob mindestens ein erweitertes Fahrzeug-Filterkriterium aktiv gesetzt ist
  private hasActiveVehicleAdvanced(): boolean {
    const f = this.appliedFilters.vehicleAdvanced;
    return !!(f.brand || f.modelName || f.color || f.identNr || f.requiredLicense
      || f.yearFrom != null || f.yearTo != null || f.powerPsFrom != null || f.powerPsTo != null
      || f.firstRegistrationFrom || f.firstRegistrationTo);
  }

  // Prüft, ob ein Fahrzeug allen aktiven Kriterien der Fahrzeug-Filterebene entspricht
  private matchesVehicleTier(v: Vehicle): boolean {
    const f = this.appliedFilters.vehicleAdvanced;
    const matchesChip = this.appliedFilters.vehicleIds.length === 0 || this.appliedFilters.vehicleIds.includes(v.Id);
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

    return matchesChip && matchesBrand && matchesModel && matchesColor && matchesYearFrom && matchesYearTo
      && matchesIdentNr && matchesLicense && matchesPowerFrom && matchesPowerTo && matchesRegFrom && matchesRegTo;
  }

  // Wendet alle aktiven Filterebenen (Fahrzeug, Telemetrie, Person) auf die Fahrzeugliste an
  get filteredVehicles(): Vehicle[] {
    // Aktive Filterebenen ermitteln
    const vehicleTierActive = this.appliedFilters.vehicleIds.length > 0 || this.hasActiveVehicleAdvanced();
    const telemetryTierActive = this.appliedFilters.telemetryUnitIds.length > 0;
    const personTierActive = this.appliedFilters.personIds.length > 0;

    return this.vehicles.filter(v => {
      // Treffer je aktiver Ebene sammeln
      const results: boolean[] = [];

      if (vehicleTierActive) results.push(this.matchesVehicleTier(v));
      if (telemetryTierActive) results.push(this.appliedFilters.telemetryUnitIds.includes(v.telemetryUnit?.id ?? ''));
      if (personTierActive) results.push(this.appliedFilters.personIds.includes(v.assignedPersonId ?? ''));

      if (results.length === 0) return true;

      // Ergebnisse je nach Modus (UND/ODER) kombinieren
      return this.appliedFilters.mode === 'union' ? results.some(Boolean) : results.every(Boolean);
    });
  }

  // Berechnet die Gesamtzahl der Seiten anhand der gefilterten Fahrzeuge
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredVehicles.length / this.pageSize));
  }

  // Liefert die Liste der anzuzeigenden Seitennummern für die Pagination
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  // Schneidet die aktuelle Seite aus der gefilterten Fahrzeugliste heraus
  get pagedVehicles(): Vehicle[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredVehicles.slice(start, start + this.pageSize);
  }

  // Erstellt die Kartenpunkte für die aktuell angezeigten bzw. ausgewählten Fahrzeuge
  get mapPoints(): VehicleMapPoint[] {
    const source = this.selectedIndex !== null
      ? [this.pagedVehicles[this.selectedIndex]]
      : this.pagedVehicles;

    return source
      .filter(v => v?.lastLocation)
      .map(v => ({
        id: v.Id,
        label: `${v.licensePlate ?? ''} — ${v.brand ?? ''} ${v.modelName ?? ''}`,
        lat: v.lastLocation!.lat,
        lng: v.lastLocation!.lng,
        timestamp: v.lastLocation!.timestamp,
        telemetryUnitId: v.lastLocation!.telemetryUnitId,
        speedKmh: v.lastLocation!.speedKmh,
        accelMs2: v.lastLocation!.accelMs2,
        driverLabel: this.getDriverTooltipLabel(v.lastLocation!.driverId),
        isMoving: this.movingVehicleIds.has(v.Id)
      }));
  }

  // Zentriert die Karte wieder auf die angezeigten Fahrzeuge
  recenterMap(): void {
    this.vehicleMap?.recenter();
  }

  // Übernimmt die aus der Filterleiste angewendeten Filter und setzt die Seite zurück
  onFiltersApplied(filters: AppliedFilters): void {
    this.appliedFilters = filters;
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  // Wechselt zur vorherigen Seite
  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Wechselt zur nächsten Seite
  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Springt direkt zu einer bestimmten Seite
  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Wählt eine Zeile aus und klappt sie auf, verwirft dabei Bearbeitungszustand
  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
    this.editError = null;
    this.deleteConfirmIndex = null;
  }

  // Klappt die aktuell ausgewählte/bearbeitete Zeile wieder zu und setzt zugehörigen Zustand zurück
  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
    this.editError = null;
    this.editSnapshot = null;
    this.deleteConfirmIndex = null;
    this.openTelemetryDropdownIndex = null;
    this.openDriverDropdownIndex = null;
    this.showAdvancedDriverFilter = false;
  }

  // Verhindert, dass ein Klick auf die Aktionsspalte den Fokus/die Auswahl verändert
  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  // Meldet dem übergeordneten Container, dass die Fahrten des Fahrzeugs angezeigt werden sollen
  onShowTrips(vehicle: Vehicle): void {
    this.showTrips.emit(vehicle.identNr ?? vehicle.Id);
  }

  // Verhindert das Schließen offener Elemente durch Klicks innerhalb der Aktionsspalte
  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  // Schaltet den Bearbeitungsmodus einer Zeile um bzw. speichert bei erneutem Aufruf
  toggleEdit(index: number): void {
    if (this.editingIndex === index) {
      this.saveEdit(index);
      return;
    }

    this.editingIndex = index;
    this.editError = null;
    this.editSnapshot = { ...this.pagedVehicles[index] };
    this.deleteConfirmIndex = null;
    this.showAdvancedDriverFilter = false;
    this.openDriverDropdownIndex = null;
  }

  // Prüft, ob das Fahrzeug gegenüber dem Bearbeitungs-Snapshot verändert wurde
  isEditDirty(vehicle: Vehicle): boolean {
    if (!this.editSnapshot) return false;

    return vehicle.licensePlate !== this.editSnapshot.licensePlate
      || vehicle.firstRegistration !== this.editSnapshot.firstRegistration
      || (vehicle.telemetryUnit?.id ?? '') !== (this.editSnapshot.telemetryUnit?.id ?? '')
      || (vehicle.assignedPersonId ?? '') !== (this.editSnapshot.assignedPersonId ?? '');
  }

  // Speichert die Änderungen der bearbeiteten Zeile über den Service
  private saveEdit(index: number): void {
    const vehicle = this.pagedVehicles[index];
    this.editError = null;

    this.vehicleService.update(vehicle).subscribe({
      next: (updated) => {
        Object.assign(vehicle, updated);
        this.editingIndex = null;
        this.editSnapshot = null;
        this.refreshTelemetryUnits();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.editError = err?.message ?? 'Aktualisieren fehlgeschlagen.';
        this.cdr.detectChanges();
      }
    });
  }

  // Fordert beim ersten Klick eine Bestätigung an und löscht das Fahrzeug beim zweiten Klick
  confirmDelete(index: number, vehicle: Vehicle): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deleteVehicle(vehicle);
  }

  // Löscht das Fahrzeug über den Service und gibt zugehörige Telemetrieeinheiten frei
  private deleteVehicle(vehicle: Vehicle): void {
    this.editError = null;

    this.vehicleService.delete(vehicle.Id).subscribe({
      next: () => {
        // Fahrzeug aus der lokalen Liste entfernen
        this.vehicles = this.vehicles.filter(v => v !== vehicle);

        // Telemetrieeinheit des gelöschten Fahrzeugs als frei markieren
        this.telemetryUnits = this.telemetryUnits.map(u =>
          u.vehicleId === vehicle.Id ? { ...u, vehicleId: undefined } : u
        );

        this.deleteConfirmIndex = null;
        this.collapseItem();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.editError = err?.message ?? 'Löschen fehlgeschlagen.';
        this.deleteConfirmIndex = null;
        this.cdr.detectChanges();
      }
    });
  }

  // Setzt die zugeordnete Telemetrieeinheit des Fahrzeugs
  onTelemetryChange(vehicle: Vehicle, value: string): void {
    vehicle.telemetryUnit = value ? { id: value } : null;
  }

  // Öffnet oder schließt das Formular zum Anlegen eines neuen Fahrzeugs und setzt es dabei zurück
  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) {
      this.newVehicle = this.emptyVehicle();
      this.createError = null;
      this.identNrError = null;
      this.yearError = null;
    }
  }

  // Formatiert das Kennzeichen des neuen Fahrzeugs (Großbuchstaben, max. 10 Zeichen)
  onNewLicensePlateChange(value: string): void {
    this.newVehicle.licensePlate = (value ?? '').toUpperCase().slice(0, 10);
  }

  // Formatiert das Kennzeichen eines bestehenden Fahrzeugs (Großbuchstaben, max. 10 Zeichen)
  onVehicleLicensePlateChange(vehicle: Vehicle, value: string): void {
    vehicle.licensePlate = (value ?? '').toUpperCase().slice(0, 10);
  }

  // Verhindert weitere Zifferneingaben, sobald die maximale Stellenanzahl erreicht ist
  onDigitFieldKeydown(event: KeyboardEvent, currentValue: number | null | undefined, maxDigits = 4): void {
    if (!/^[0-9]$/.test(event.key)) return;

    const currentLength = currentValue != null ? currentValue.toString().length : 0;
    if (currentLength >= maxDigits) {
      event.preventDefault();
    }
  }

  // Verhindert negative Leistungsangaben und begrenzt die Stellenanzahl der Leistung (PS)
  onPowerPsKeydown(event: KeyboardEvent): void {
    if (event.key === '-') {
      event.preventDefault();
      return;
    }

    this.onDigitFieldKeydown(event, this.newVehicle.powerPs);
  }

  // Übernimmt das eingegebene Baujahr und validiert es
  onYearChange(value: number | null): void {
    this.newVehicle.year = value ?? undefined;
    this.yearError = this.validateYear(value);
  }

  // Prüft, ob das Baujahr im gültigen Bereich (ab 1981) liegt
  private validateYear(value: number | null | undefined): string | null {
    if (value == null) return null;

    return value >= 1981 ? null : 'Baujahr muss 1981 oder später sein.';
  }

  // Bereinigt die eingegebene Ident.-Nr. (Großbuchstaben, keine Leer-/Trennzeichen) und validiert sie
  onIdentNrChange(value: string): void {
    const sanitized = (value ?? '').toUpperCase().replace(/[\s-]/g, '');
    this.newVehicle.identNr = sanitized;
    this.identNrError = this.validateIdentNr(sanitized);
  }

  // Prüft die Ident.-Nr. auf Länge (17 Zeichen) und verbotene Zeichen (I, O, Q)
  private validateIdentNr(value: string): string | null {
    if (!value) return null;

    if (value.length !== 17) {
      return 'Ident.-Nr. muss genau 17 Zeichen haben.';
    }

    if (/[IOQ]/.test(value)) {
      return 'Ident.-Nr. ungültig (I, O, Q nicht erlaubt).';
    }

    return null;
  }

  // Validiert das neue Fahrzeug und legt es über den Service an
  createVehicle(): void {
    // Pflichtfelder prüfen
    if (!this.newVehicle.modelName || !this.newVehicle.identNr || !this.newVehicle.brand
      || !this.newVehicle.year || !this.newVehicle.requiredLicense
      || !this.newVehicle.powerPs || !this.newVehicle.color) return;

    // Ident.-Nr. validieren
    this.identNrError = this.validateIdentNr(this.newVehicle.identNr);
    if (this.identNrError) {
      this.createError = this.identNrError;
      return;
    }

    // Baujahr validieren
    this.yearError = this.validateYear(this.newVehicle.year);
    if (this.yearError) {
      this.createError = this.yearError;
      return;
    }

    this.createError = null;

    // Fahrzeug speichern
    this.vehicleService.save(this.newVehicle).subscribe({
      next: (created) => {
        this.vehicles.unshift(created);
        this.newVehicle = this.emptyVehicle();
        this.identNrError = null;
        this.showCreateForm = false;
        this.currentPage = 1;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.createError = err?.message ?? 'Speichern fehlgeschlagen.';
        this.cdr.detectChanges();
      }
    });
  }

  // Erstellt ein leeres Fahrzeug-Objekt als Ausgangszustand für das Anlegeformular
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
}
