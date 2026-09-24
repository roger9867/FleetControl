import { Component, OnInit, OnDestroy, ChangeDetectorRef, HostListener, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { FilterComponent, AppliedFilters, emptyAppliedFilters } from '../filter-sidebar/filter-sidebar.component';
import { TelemetryUnitService } from '../../services/telemetry-unit.service';
import { VehicleService } from '../../services/vehicle.service';
import { PersonService } from '../../services/person.service';
import { UsbTelemetryService } from '../../services/usb-telemetry.service';
import { TelemetryUnit } from '../../models/telemetry-unit.model';
import { Vehicle } from '../../models/vehicle.model';
import { Person } from '../../models/person.model';

@Component({
  selector: 'app-telemetry-units',
  standalone: true,
  imports: [CommonModule, FormsModule, FilterComponent],
  templateUrl: './telemetry-units.component.html',
  styleUrls: ['./telemetry-units.component.scss']
})
export class TelemetryUnitPageComponent implements OnInit, OnDestroy
{
  @Output() showTrips = new EventEmitter<string>();

  usb_connected_units: TelemetryUnit[] = [];
  private broadcastSub?: Subscription;

  registered_units: TelemetryUnit[] = [];
  vehicles: Vehicle[] = [];
  persons: Person[] = [];

  selectedIndex: number | null = null;
  editingIndex: number | null = null;
  deleteConfirmIndex: number | null = null;
  selectedUnitId: string | null = null;

  pageSize = 10;
  currentPage = 1;

  editError: string | null = null;
  editSnapshot: TelemetryUnit | null = null;

  appliedFilters: AppliedFilters = emptyAppliedFilters();

  openVehicleDropdownIndex: number | null = null;

  // Injiziert die benötigten Services für Telemetrie-, Fahrzeug- und Personendaten
  constructor(
    private service: TelemetryUnitService,
    private vehicleService: VehicleService,
    private personService: PersonService,
    private usbTelemetryService: UsbTelemetryService,
    private cdr: ChangeDetectorRef
  ) {

  }

  // Schliesst offene Dropdowns (Löschen-Bestätigung, Fahrzeugauswahl) bei Klick ausserhalb
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.deleteConfirmIndex !== null) {
      const target = event.target as HTMLElement;
      if (!target.closest || !target.closest('.delete-btn')) {
        this.deleteConfirmIndex = null;
      }
    }

    if (this.openVehicleDropdownIndex !== null) {
      const target = event.target as HTMLElement;
      if (!target.closest || !target.closest('.unit-select')) {
        this.openVehicleDropdownIndex = null;
      }
    }
  }

  // Initialisiert die Komponente: lädt Einheiten, abonniert USB-Broadcasts und lädt Stammdaten
  ngOnInit(): void {
    // 1. Registrierte Telemetrie-Einheiten laden
    this.loadAllUnits();

    // 2. Verbundene USB-Einheiten laufend abonnieren
    this.broadcastSub = this.usbTelemetryService.connectedUnitIds$
      .subscribe(uuids => {
        this.usb_connected_units = uuids.map(id => ({ id }));
        this.cdr.detectChanges();
      });

    // 3. Fahrzeuge für die Zuordnung laden
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        if (vehicles?.length) {
          this.vehicles = vehicles;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // 4. Personen für die Zuordnung laden
    this.personService.loadAll().subscribe({
      next: (persons) => {
        if (persons?.length) {
          this.persons = persons;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Registriert die ausgewählte Telemetrie-Einheit über den Service
  createUnit(): void {
    // 1. Auswahl prüfen und DTO aufbauen
    if (!this.selectedUnitId || this.isUnitAlreadyRegistered(this.selectedUnitId)) return;
    const dto = {
      id: this.selectedUnitId
    };

    // 2. Neue Einheit anlegen und Liste danach neu laden
    this.service.createUnit(dto)
      .subscribe({
        next: () => {
          console.log('CREATED');
          this.loadAllUnits();
          this.selectedUnitId = null;
        },
        error: (err) => {
          console.error('CREATE FAILED', err);
        }
      });
  }

  // Lädt alle registrierten Telemetrie-Einheiten vom Server
  loadAllUnits(): void {
    this.service.getUnits()
      .subscribe({
        next: (res) => {
          this.registered_units = res ?? [];
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('GET FAILED', err);
          this.registered_units = [];
        }
      });
  }

  // Sendet einen Broadcast an alle verbundenen USB-Telemetrie-Geräte
  async sendBroadcast(): Promise<void> {
    try {
      await this.usbTelemetryService.broadcastToAll();
    } catch (err: any) {
      if (err?.name === 'NotFoundError') return;
      console.error('Serial-Broadcast fehlgeschlagen', err);
    }
  }

  // Beendet das Broadcast-Abonnement beim Zerstören der Komponente
  ngOnDestroy(): void {
    this.broadcastSub?.unsubscribe();
  }

  // Liefert die IDs aller registrierten Telemetrie-Einheiten
  get telemetryUnitIds(): string[] {
    return this.registered_units.map(u => u.id);
  }

  // Prüft, ob eine per USB gefundene Geräte-ID bereits als Telemetrie-Einheit registriert ist.
  // Vergleich erfolgt case-insensitiv, da die Firmware die UUID in Großbuchstaben liefert,
  // das Backend (.NET Guid) sie aber kleingeschrieben zurückgibt.
  isUnitAlreadyRegistered(id: string): boolean {
    const normalized = id.toLowerCase();
    return this.telemetryUnitIds.some(existingId => existingId.toLowerCase() === normalized);
  }

  // Wendet die aktiven Filterkriterien (Einheit, Fahrzeug, Person) auf die Einheitenliste an
  get filteredUnits(): TelemetryUnit[] {
    // 1. Aktive Filterebenen bestimmen
    const unitTierActive = this.appliedFilters.telemetryUnitIds.length > 0;
    const vehicleTierActive = this.appliedFilters.vehicleIds.length > 0;
    const personTierActive = this.appliedFilters.personIds.length > 0;

    return this.registered_units.filter(u => {
      const results: boolean[] = [];

      // 2. Treffer je aktiver Filterebene sammeln
      if (unitTierActive) results.push(this.appliedFilters.telemetryUnitIds.includes(u.id));
      if (vehicleTierActive) results.push(this.appliedFilters.vehicleIds.includes(u.vehicleId ?? ''));

      if (personTierActive) {
        const assignedVehicle = this.vehicles.find(v => v.Id === u.vehicleId);
        results.push(this.appliedFilters.personIds.includes(assignedVehicle?.assignedPersonId ?? ''));
      }

      if (results.length === 0) return true;

      // 3. Ergebnisse je nach Modus (Schnittmenge/Vereinigung) kombinieren
      return this.appliedFilters.mode === 'union' ? results.some(Boolean) : results.every(Boolean);
    });
  }

  // Berechnet die Gesamtzahl der Seiten anhand der gefilterten Einheiten
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredUnits.length / this.pageSize));
  }

  // Liefert die Liste der Seitenzahlen für die Pagination
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  // Liefert die Einheiten der aktuell angezeigten Seite
  get pagedUnits(): TelemetryUnit[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredUnits.slice(start, start + this.pageSize);
  }

  // Wechselt zur vorherigen Seite und schliesst offene Auswahl/Bearbeitung
  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Wechselt zur nächsten Seite und schliesst offene Auswahl/Bearbeitung
  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Springt zu einer bestimmten Seite und schliesst offene Auswahl/Bearbeitung
  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Übernimmt neu angewendete Filter und setzt Seite sowie Auswahl zurück
  onFiltersApplied(filters: AppliedFilters): void {
    this.appliedFilters = filters;
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  // Wählt einen Listeneintrag aus und schliesst andere offene Zustände
  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
    this.editError = null;
    this.deleteConfirmIndex = null;
  }

  // Klappt den ausgewählten Eintrag ein und setzt alle zugehörigen Zustände zurück
  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
    this.editError = null;
    this.editSnapshot = null;
    this.deleteConfirmIndex = null;
    this.openVehicleDropdownIndex = null;
  }

  // Verhindert, dass ein Mousedown auf den Aktionen den Fokus verliert
  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  // Löst die Anzeige der Fahrten für die gewählte Einheit aus
  onShowTrips(item: TelemetryUnit): void {
    this.showTrips.emit(item.id);
  }

  // Verhindert das Schliessen der Löschen-Bestätigung bei Klick innerhalb der Aktionen
  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  // Wechselt zwischen Anzeige- und Bearbeitungsmodus eines Eintrags bzw. speichert bei erneutem Klick
  toggleEdit(index: number): void {
    if (this.editingIndex === index) {
      this.saveEdit(index);
      return;
    }

    this.editingIndex = index;
    this.editError = null;
    this.editSnapshot = { ...this.pagedUnits[index] };
    this.deleteConfirmIndex = null;
  }

  // Prüft, ob sich die Fahrzeugzuordnung gegenüber dem gespeicherten Zustand geändert hat
  isEditDirty(unit: TelemetryUnit): boolean {
    if (!this.editSnapshot) return false;

    return (unit.vehicleId ?? '') !== (this.editSnapshot.vehicleId ?? '');
  }

  // Prüft, ob ein Fahrzeug bereits einer anderen Telemetrie-Einheit zugeordnet ist
  isVehicleTakenByOtherUnit(vehicle: Vehicle, unit: TelemetryUnit): boolean {
    return this.registered_units.some(u => u.id !== unit.id && u.vehicleId === vehicle.Id);
  }

  // Öffnet oder schliesst das Fahrzeugauswahl-Dropdown für einen Eintrag
  toggleVehicleDropdown(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.openVehicleDropdownIndex = this.openVehicleDropdownIndex === index ? null : index;
  }

  // Weist der Telemetrie-Einheit das gewählte Fahrzeug zu, sofern nicht deaktiviert
  selectVehicleForUnit(unit: TelemetryUnit, vehicleId: string, disabled = false): void {
    if (disabled) return;

    unit.vehicleId = vehicleId || undefined;
    this.openVehicleDropdownIndex = null;
  }

  // Speichert die Änderungen des bearbeiteten Eintrags über den Service
  private saveEdit(index: number): void {
    // 1. Zu speichernde Einheit ermitteln
    const unit = this.pagedUnits[index];
    this.editError = null;

    // 2. Änderungen speichern und Bearbeitungsmodus beenden
    this.service.update(unit).subscribe({
      next: (updated) => {
        Object.assign(unit, updated);
        this.editingIndex = null;
        this.editSnapshot = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.editError = err?.message ?? 'Aktualisieren fehlgeschlagen.';
        this.cdr.detectChanges();
      }
    });
  }

  // Fordert beim ersten Klick eine Bestätigung an und löscht erst beim zweiten Klick
  confirmDelete(index: number, unit: TelemetryUnit): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deleteUnit(unit);
  }

  // Löscht die Telemetrie-Einheit über den Service und aktualisiert die lokale Liste
  private deleteUnit(unit: TelemetryUnit): void {
    this.editError = null;

    // Löschen anstossen und bei Erfolg die Liste sowie offene Zustände bereinigen
    this.service.delete(unit.id).subscribe({
      next: () => {
        this.registered_units = this.registered_units.filter(u => u !== unit);
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
}
