import { Component, OnInit, OnDestroy, ChangeDetectorRef, HostListener, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { FilterSidebar, AppliedFilters, emptyAppliedFilters } from '../filter-sidebar/filter-sidebar.component';
import { TelemetryUnitService } from '../../services/telemetry-unit.service';
import { VehicleService } from '../../services/vehicle.service';
import { PersonService } from '../../services/person.service';
import { VehicleLiveService } from '../../services/vehicle-live.service';
import { TelemetryUnit } from '../../models/telemetry-unit.model';
import { Vehicle } from '../../models/vehicle.model';
import { Person } from '../../models/person.model';

@Component({
  selector: 'app-telemetry-units',
  standalone: true,
  imports: [CommonModule, FormsModule, FilterSidebar],
  templateUrl: './telemetry-units.component.html',
  styleUrls: ['./telemetry-units.component.scss']
})
export class TelemetryUnits implements OnInit, OnDestroy
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

  constructor(
    private service: TelemetryUnitService,
    private vehicleService: VehicleService,
    private personService: PersonService,
    private vehicleLiveService: VehicleLiveService,
    private cdr: ChangeDetectorRef
  ) {

  }

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

  ngOnInit(): void {
    this.loadAllUnits();

    this.broadcastSub = this.vehicleLiveService.usbUnitsChanged$
      .subscribe(uuids => {
        this.usb_connected_units = uuids.map(id => ({ id }));
        this.cdr.detectChanges();
      });

    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        if (vehicles?.length) {
          this.vehicles = vehicles;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });

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

  createUnit(): void {
    console.log('BUTTON CLICKED');

    if (!this.selectedUnitId) return;
    console.log('NO UNIT SELECTED');
    const dto = {
      id: this.selectedUnitId
    };

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

  sendBroadcast(): void {
    this.service.broadcastCommand()
      .subscribe(res => {
        this.usb_connected_units = this.service.filterUuidResponses(res).map(id => ({ id }));
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.broadcastSub?.unsubscribe();
  }

  get telemetryUnitIds(): string[] {
    return this.registered_units.map(u => u.id);
  }

  get filteredUnits(): TelemetryUnit[] {
    const unitTierActive = this.appliedFilters.telemetryUnitIds.length > 0;
    const vehicleTierActive = this.appliedFilters.vehicleIds.length > 0;
    const personTierActive = this.appliedFilters.personIds.length > 0;

    return this.registered_units.filter(u => {
      const results: boolean[] = [];

      if (unitTierActive) results.push(this.appliedFilters.telemetryUnitIds.includes(u.id));
      if (vehicleTierActive) results.push(this.appliedFilters.vehicleIds.includes(u.vehicleId ?? ''));

      if (personTierActive) {
        const assignedVehicle = this.vehicles.find(v => v.Id === u.vehicleId);
        results.push(this.appliedFilters.personIds.includes(assignedVehicle?.assignedPersonId ?? ''));
      }

      if (results.length === 0) return true;

      return this.appliedFilters.mode === 'union' ? results.some(Boolean) : results.every(Boolean);
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredUnits.length / this.pageSize));
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  get pagedUnits(): TelemetryUnit[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredUnits.slice(start, start + this.pageSize);
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

  onFiltersApplied(filters: AppliedFilters): void {
    this.appliedFilters = filters;
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
    this.editError = null;
    this.deleteConfirmIndex = null;
  }

  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
    this.editError = null;
    this.editSnapshot = null;
    this.deleteConfirmIndex = null;
    this.openVehicleDropdownIndex = null;
  }

  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  onShowTrips(item: TelemetryUnit): void {
    this.showTrips.emit(item.id);
  }

  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

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

  isEditDirty(unit: TelemetryUnit): boolean {
    if (!this.editSnapshot) return false;

    return (unit.vehicleId ?? '') !== (this.editSnapshot.vehicleId ?? '');
  }

  isVehicleTakenByOtherUnit(vehicle: Vehicle, unit: TelemetryUnit): boolean {
    return this.registered_units.some(u => u.id !== unit.id && u.vehicleId === vehicle.Id);
  }

  toggleVehicleDropdown(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.openVehicleDropdownIndex = this.openVehicleDropdownIndex === index ? null : index;
  }

  selectVehicleForUnit(unit: TelemetryUnit, vehicleId: string, disabled = false): void {
    if (disabled) return;

    unit.vehicleId = vehicleId || undefined;
    this.openVehicleDropdownIndex = null;
  }

  private saveEdit(index: number): void {
    const unit = this.pagedUnits[index];
    this.editError = null;

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

  confirmDelete(index: number, unit: TelemetryUnit): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deleteUnit(unit);
  }

  private deleteUnit(unit: TelemetryUnit): void {
    this.editError = null;

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
