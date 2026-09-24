import { Component, OnInit, ChangeDetectorRef, HostListener, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { FilterComponent, AppliedFilters, emptyAppliedFilters } from '../filter-sidebar/filter-sidebar.component';
import { Person } from '../../models/person.model';
import { Vehicle } from '../../models/vehicle.model';
import { PersonService } from '../../services/person.service';
import { VehicleService } from '../../services/vehicle.service';
import { TelemetryUnitService } from '../../services/telemetry-unit.service';

@Component({
  selector: 'app-personen',
  standalone: true,
  imports: [CommonModule, FormsModule, FilterComponent],
  templateUrl: './personen.component.html',
  styleUrls: ['./personen.component.scss']
})
export class PersonPageComponent implements OnInit
{
  @Output() showTrips = new EventEmitter<string>();

  persons: Person[] = [];
  vehicles: Vehicle[] = [];
  telemetryUnitIds: string[] = [];

  pageSize = 10;
  currentPage = 1;

  showCreateForm = false;
  newPerson: Person = this.emptyPerson();
  newLicenseClass = '';
  newLicenseDate = '';
  showNewVehicleDropdown = false;

  selectedIndex: number | null = null;
  editingIndex: number | null = null;
  deleteConfirmIndex: number | null = null;
  editLicenseClass = '';
  editLicenseDate = '';

  createError: string | null = null;
  editError: string | null = null;
  editSnapshot: Person | null = null;

  licenseClasses: string[] = [
    'AM', 'A1', 'A2', 'A', 'B', 'BE',
    'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE'
  ];

  appliedFilters: AppliedFilters = emptyAppliedFilters();

  // Liefert die noch nicht vergebenen Führerscheinklassen für die neue Person
  get availableNewLicenseClasses(): string[] {
    const taken = new Set((this.newPerson.licenses ?? []).map(l => l.licenseClass));
    return this.licenseClasses.filter(c => !taken.has(c));
  }

  // Liefert die noch nicht vergebenen Führerscheinklassen für die bearbeitete Person
  availableEditLicenseClasses(person: Person): string[] {
    const taken = new Set((person.licenses ?? []).map(l => l.licenseClass));
    return this.licenseClasses.filter(c => !taken.has(c));
  }

  // Initialisiert die Komponente mit den benötigten Services
  constructor(
    private personService: PersonService,
    private vehicleService: VehicleService,
    private telemetryUnitService: TelemetryUnitService,
    private cdr: ChangeDetectorRef
  ) {}

  // Schliesst offene Lösch-Bestätigung und Fahrzeug-Dropdown bei Klick außerhalb
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    // Lösch-Bestätigung schließen, wenn außerhalb des Lösch-Buttons geklickt wurde
    if (this.deleteConfirmIndex !== null) {
      const target = event.target as HTMLElement;
      if (!target.closest || !target.closest('.delete-btn')) {
        this.deleteConfirmIndex = null;
      }
    }

    // Fahrzeug-Dropdown schließen, wenn außerhalb der Auswahl geklickt wurde
    if (this.showNewVehicleDropdown) {
      const target = event.target as HTMLElement;
      if (!target.closest || !target.closest('.vehicle-select')) {
        this.showNewVehicleDropdown = false;
      }
    }
  }

  // Lädt beim Start Personen, Fahrzeuge und Telemetrieeinheiten vom Server
  ngOnInit(): void {
    // Personen laden
    this.personService.loadAll().subscribe({
      next: (persons) => {
        if (persons?.length) {
          this.persons = persons;
        }
        this.syncAssignedVehicles();
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // Fahrzeuge laden
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        if (vehicles?.length) {
          this.vehicles = vehicles;
        }
        this.syncAssignedVehicles();
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // Telemetrieeinheiten laden
    this.telemetryUnitService.getUnits().subscribe({
      next: (units) => {
        if (units?.length) {
          this.telemetryUnitIds = units.map(u => u.id);
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Lädt die Fahrzeugliste neu, damit assignedPersonId nach Zuordnungsänderungen aktuell ist
  private reloadVehicles(): void {
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        if (vehicles?.length) {
          this.vehicles = vehicles;
        }
        this.syncAssignedVehicles();
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Verknüpft jede Person mit ihrem zugewiesenen Fahrzeug anhand der Fahrzeugdaten
  private syncAssignedVehicles(): void {
    for (const person of this.persons) {
      const assignedVehicle = this.vehicles.find(v => v.assignedPersonId === person.Id);
      person.assignedVehicleId = assignedVehicle?.Id ?? null;
    }
  }

  // Prüft, ob erweiterte Personenfilter (Vor-/Nachname, Personalnummer) gesetzt sind
  private hasActivePersonAdvanced(): boolean {
    const f = this.appliedFilters.personAdvanced;
    return !!(f.firstName || f.lastName || f.employeeNr);
  }

  // Prüft, ob eine Person zu den aktiven Personenfiltern passt
  private matchesPersonTier(p: Person): boolean {
    const f = this.appliedFilters.personAdvanced;
    const matchesChip = this.appliedFilters.personIds.length === 0 || this.appliedFilters.personIds.includes(p.Id);
    const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
    const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
    const matchesEmployeeNr = !f.employeeNr || p.Id.toLowerCase().includes(f.employeeNr.toLowerCase());

    return matchesChip && matchesFirstName && matchesLastName && matchesEmployeeNr;
  }

  // Liefert die Personen, die den aktuell angewendeten Filtern (Personen-, Fahrzeug-, Telemetrieebene) entsprechen
  get filteredPersons(): Person[] {
    // Aktive Filterebenen ermitteln
    const personTierActive = this.appliedFilters.personIds.length > 0 || this.hasActivePersonAdvanced();
    const vehicleTierActive = this.appliedFilters.vehicleIds.length > 0;
    const telemetryTierActive = this.appliedFilters.telemetryUnitIds.length > 0;

    return this.persons.filter(p => {
      const results: boolean[] = [];

      // Treffer je aktiver Filterebene sammeln
      if (personTierActive) results.push(this.matchesPersonTier(p));
      if (vehicleTierActive) results.push(this.appliedFilters.vehicleIds.includes(p.assignedVehicleId ?? ''));

      if (telemetryTierActive) {
        const assignedVehicle = this.vehicles.find(v => v.Id === p.assignedVehicleId);
        results.push(this.appliedFilters.telemetryUnitIds.includes(assignedVehicle?.telemetryUnit?.id ?? ''));
      }

      if (results.length === 0) return true;

      // Ergebnisse je nach Modus (Vereinigung oder Schnittmenge) kombinieren
      return this.appliedFilters.mode === 'union' ? results.some(Boolean) : results.every(Boolean);
    });
  }

  // Berechnet die Gesamtanzahl der Seiten anhand der gefilterten Personen
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredPersons.length / this.pageSize));
  }

  // Liefert die Liste der Seitenzahlen für die Paginierung
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  // Liefert die Personen der aktuell angezeigten Seite
  get pagedPersons(): Person[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredPersons.slice(start, start + this.pageSize);
  }

  // Übernimmt neu angewendete Filter und setzt die Ansicht zurück
  onFiltersApplied(filters: AppliedFilters): void {
    this.appliedFilters = filters;
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  // Wechselt zur vorherigen Seite und setzt die Auswahl zurück
  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  // Wechselt zur nächsten Seite und setzt die Auswahl zurück
  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  // Springt zu einer bestimmten Seite und setzt die Auswahl zurück
  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  // Markiert einen Eintrag als ausgewählt und beendet Bearbeitung/Löschbestätigung
  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
    this.editError = null;
    this.deleteConfirmIndex = null;
  }

  // Wählt einen Eintrag per Klick aus, außer er wird gerade bearbeitet
  onBoxClick(index: number): void {
    if (this.editingIndex === index) return;

    this.selectItem(index);
  }

  // Klappt den ausgewählten Eintrag wieder ein und setzt alle zugehörigen Zustände zurück
  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
    this.editError = null;
    this.editSnapshot = null;
    this.deleteConfirmIndex = null;
  }

  // Verhindert das Standardverhalten beim Drücken der Maustaste auf Aktionsbuttons
  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  // Meldet die Anfrage, die Fahrten einer Person anzuzeigen, an die übergeordnete Komponente
  onShowTrips(person: Person): void {
    this.showTrips.emit(person.Id);
  }

  // Verhindert das Schließen der Auswahl beim Klick auf den Aktionsbereich und schließt ggf. die Löschbestätigung
  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  // Schaltet den Bearbeitungsmodus eines Eintrags um oder speichert die Änderungen
  toggleEdit(index: number): void {
    if (this.editingIndex === index) {
      this.saveEdit(index);
      return;
    }

    // Bearbeitungsmodus starten und Ausgangszustand als Snapshot sichern
    this.editingIndex = index;
    this.editError = null;
    this.editSnapshot = { ...this.pagedPersons[index], licenses: [...(this.pagedPersons[index].licenses ?? [])] };
    this.editLicenseClass = '';
    this.editLicenseDate = '';
    this.deleteConfirmIndex = null;
  }

  // Prüft, ob die bearbeitete Person vom ursprünglichen Snapshot abweicht
  isEditDirty(person: Person): boolean {
    if (!this.editSnapshot) return false;

    return person.firstName !== this.editSnapshot.firstName
      || person.lastName !== this.editSnapshot.lastName
      || person.birthDate !== this.editSnapshot.birthDate
      || (person.assignedVehicleId ?? '') !== (this.editSnapshot.assignedVehicleId ?? '')
      || JSON.stringify(person.licenses ?? []) !== JSON.stringify(this.editSnapshot.licenses ?? []);
  }

  // Speichert die Änderungen der bearbeiteten Person über den Server
  private saveEdit(index: number): void {
    const person = this.pagedPersons[index];

    this.editError = null;

    this.personService.update(person).subscribe({
      next: (updated) => {
        Object.assign(person, updated);
        this.editingIndex = null;
        this.editSnapshot = null;
        this.reloadVehicles();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.editError = err?.message ?? 'Aktualisieren fehlgeschlagen.';
        this.cdr.detectChanges();
      }
    });
  }

  // Fordert beim ersten Klick eine Bestätigung an und löscht die Person beim zweiten Klick
  confirmDelete(index: number, person: Person): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deletePerson(person);
  }

  // Löscht die Person über den Server und entfernt sie aus der lokalen Liste
  private deletePerson(person: Person): void {
    this.editError = null;

    this.personService.delete(person.Id).subscribe({
      next: () => {
        this.persons = this.persons.filter(p => p !== person);
        this.deleteConfirmIndex = null;
        this.collapseItem();
        this.reloadVehicles();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.editError = err?.message ?? 'Löschen fehlgeschlagen.';
        this.deleteConfirmIndex = null;
        this.cdr.detectChanges();
      }
    });
  }

  // Weist der Person ein neues Fahrzeug zu oder entfernt die Zuweisung
  onVehicleAssignmentChange(person: Person, value: string): void {
    person.assignedVehicleId = value || null;
  }

  // Prüft, ob ein Fahrzeug bereits einer anderen Person zugewiesen ist
  isVehicleTakenByOtherPerson(vehicle: Vehicle, person: Person): boolean {
    return !!vehicle.assignedPersonId && vehicle.assignedPersonId !== person.Id;
  }

  // Erzeugt die Anzeigebezeichnung für ein Fahrzeug anhand seiner Id
  vehicleLabel(vehicleId: string | null | undefined): string {
    if (!vehicleId) return 'Kein Fahrzeug';

    const vehicle = this.vehicles.find(v => v.Id === vehicleId);
    return vehicle ? `${vehicle.Id} — ${vehicle.brand ?? ''} ${vehicle.modelName ?? ''}`.trim() : vehicleId;
  }

  // Öffnet oder schließt das Fahrzeug-Auswahl-Dropdown für die neue Person
  toggleNewVehicleDropdown(): void {
    this.showNewVehicleDropdown = !this.showNewVehicleDropdown;
  }

  // Wählt ein Fahrzeug für die neue Person aus, sofern es nicht gesperrt ist
  selectNewPersonVehicle(vehicleId: string | null, disabled = false): void {
    if (disabled) return;

    this.newPerson.assignedVehicleId = vehicleId;
    this.showNewVehicleDropdown = false;
  }

  // Fügt der neuen Person einen Führerschein hinzu, falls Klasse und Datum gültig sind
  addLicenseToNewPerson(): void {
    if (!this.newLicenseClass || !this.newLicenseDate) return;

    this.newPerson.licenses = this.newPerson.licenses ?? [];
    if (this.newPerson.licenses.some(l => l.licenseClass === this.newLicenseClass)) return;

    this.newPerson.licenses.push({ licenseClass: this.newLicenseClass, obtainedDate: this.newLicenseDate });
    this.newLicenseClass = '';
    this.newLicenseDate = '';
  }

  // Entfernt einen Führerschein von der neuen Person
  removeLicenseFromNewPerson(licenseClass: string): void {
    this.newPerson.licenses = (this.newPerson.licenses ?? []).filter(l => l.licenseClass !== licenseClass);
  }

  // Fügt einer bestehenden Person einen Führerschein hinzu, falls Klasse und Datum gültig sind
  addLicenseToPerson(person: Person): void {
    if (!this.editLicenseClass || !this.editLicenseDate) return;

    person.licenses = person.licenses ?? [];
    if (person.licenses.some(l => l.licenseClass === this.editLicenseClass)) return;

    person.licenses.push({ licenseClass: this.editLicenseClass, obtainedDate: this.editLicenseDate });
    this.editLicenseClass = '';
    this.editLicenseDate = '';
  }

  // Entfernt einen Führerschein von einer bestehenden Person
  removeLicenseFromPerson(person: Person, licenseClass: string): void {
    person.licenses = (person.licenses ?? []).filter(l => l.licenseClass !== licenseClass);
  }

  // Öffnet oder schliesst das Formular zum Anlegen einer neuen Person
  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) {
      this.newPerson = this.emptyPerson();
      this.newLicenseClass = '';
      this.newLicenseDate = '';
      this.createError = null;
      this.showNewVehicleDropdown = false;
    }
  }

  // Legt eine neue Person über den Server an, sofern die Pflichtfelder ausgefüllt sind
  createPerson(): void {
    if (!this.newPerson.firstName || !this.newPerson.lastName || !this.newPerson.birthDate) return;

    this.createError = null;

    this.personService.save(this.newPerson).subscribe({
      next: (created) => {
        this.persons.unshift(created);
        this.newPerson = this.emptyPerson();
        this.showNewVehicleDropdown = false;
        this.showCreateForm = false;
        this.currentPage = 1;
        this.reloadVehicles();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.createError = err?.message ?? 'Speichern fehlgeschlagen.';
        this.cdr.detectChanges();
      }
    });
  }

  // Erzeugt eine leere Person als Vorlage für das Neu-Anlegen-Formular
  private emptyPerson(): Person {
    return {
      Id: '',
      firstName: '',
      lastName: '',
      birthDate: '',
      licenses: [],
      assignedVehicleId: null
    };
  }
}
