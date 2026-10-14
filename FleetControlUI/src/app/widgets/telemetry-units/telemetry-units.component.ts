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

  // Injects the services required for telemetry, vehicle and person data
  constructor(
    private service: TelemetryUnitService,
    private vehicleService: VehicleService,
    private personService: PersonService,
    private usbTelemetryService: UsbTelemetryService,
    private cdr: ChangeDetectorRef
  ) {

  }

  // Closes open dropdowns (delete confirmation, vehicle selection) on a click outside
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

  // Initializes the component: loads units, subscribes to USB broadcasts and loads master data
  ngOnInit(): void {
    // 1. Load registered telemetry units
    this.loadAllUnits();

    // 2. Continuously subscribe to connected USB units
    this.broadcastSub = this.usbTelemetryService.connectedUnitIds$
      .subscribe(uuids => {
        this.usb_connected_units = uuids.map(id => ({ id }));
        this.cdr.detectChanges();
      });

    // 3. Load vehicles for the assignment
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        if (vehicles?.length) {
          this.vehicles = vehicles;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // 4. Load persons for the assignment
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

  // Registers the selected telemetry unit via the service
  createUnit(): void {
    // 1. Check the selection and build the DTO
    if (!this.selectedUnitId || this.isUnitAlreadyRegistered(this.selectedUnitId)) return;
    const dto = {
      id: this.selectedUnitId
    };

    // 2. Create the new unit and reload the list afterwards
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

  // Loads all registered telemetry units from the server
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

  // Sends a broadcast to all connected USB telemetry devices
  async sendBroadcast(): Promise<void> {
    try {
      await this.usbTelemetryService.broadcastToAll();
    } catch (err: any) {
      if (err?.name === 'NotFoundError') return;
      console.error('Serial-Broadcast fehlgeschlagen', err);
    }
  }

  // Ends the broadcast subscription when the component is destroyed
  ngOnDestroy(): void {
    this.broadcastSub?.unsubscribe();
  }

  // Returns the IDs of all registered telemetry units
  get telemetryUnitIds(): string[] {
    return this.registered_units.map(u => u.id);
  }

  // Checks whether a device ID found via USB is already registered as a telemetry unit.
  // The comparison is case-insensitive because the firmware returns the UUID in upper case,
  // while the backend (.NET Guid) returns it in lower case.
  isUnitAlreadyRegistered(id: string): boolean {
    const normalized = id.toLowerCase();
    return this.telemetryUnitIds.some(existingId => existingId.toLowerCase() === normalized);
  }

  // Applies the active filter criteria (unit, vehicle, person) to the unit list
  get filteredUnits(): TelemetryUnit[] {
    // 1. Determine active filter levels
    const unitTierActive = this.appliedFilters.telemetryUnitIds.length > 0;
    const vehicleTierActive = this.appliedFilters.vehicleIds.length > 0;
    const personTierActive = this.appliedFilters.personIds.length > 0;

    return this.registered_units.filter(u => {
      const results: boolean[] = [];

      // 2. Collect matches per active filter level
      if (unitTierActive) results.push(this.appliedFilters.telemetryUnitIds.includes(u.id));
      if (vehicleTierActive) results.push(this.appliedFilters.vehicleIds.includes(u.vehicleId ?? ''));

      if (personTierActive) {
        const assignedVehicle = this.vehicles.find(v => v.Id === u.vehicleId);
        results.push(this.appliedFilters.personIds.includes(assignedVehicle?.assignedPersonId ?? ''));
      }

      if (results.length === 0) return true;

      // 3. Combine results depending on the mode (intersection/union)
      return this.appliedFilters.mode === 'union' ? results.some(Boolean) : results.every(Boolean);
    });
  }

  // Calculates the total number of pages from the filtered units
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredUnits.length / this.pageSize));
  }

  // Returns the list of page numbers for the pagination
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  // Returns the units of the currently displayed page
  get pagedUnits(): TelemetryUnit[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredUnits.slice(start, start + this.pageSize);
  }

  // Switches to the previous page and closes open selection/editing
  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Switches to the next page and closes open selection/editing
  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Jumps to a specific page and closes open selection/editing
  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Takes over newly applied filters and resets page and selection
  onFiltersApplied(filters: AppliedFilters): void {
    this.appliedFilters = filters;
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  // Selects a list entry and closes other open states
  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
    this.editError = null;
    this.deleteConfirmIndex = null;
  }

  // Collapses the selected entry and resets all related state
  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
    this.editError = null;
    this.editSnapshot = null;
    this.deleteConfirmIndex = null;
    this.openVehicleDropdownIndex = null;
  }

  // Prevents a mousedown on the actions from losing focus
  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  // Triggers showing the trips for the selected unit
  onShowTrips(item: TelemetryUnit): void {
    this.showTrips.emit(item.id);
  }

  // Prevents the delete confirmation from closing when clicking inside the actions
  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  // Switches between view and edit mode of an entry, or saves when clicked again
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

  // Checks whether the vehicle assignment has changed compared to the saved state
  isEditDirty(unit: TelemetryUnit): boolean {
    if (!this.editSnapshot) return false;

    return (unit.vehicleId ?? '') !== (this.editSnapshot.vehicleId ?? '');
  }

  // Checks whether a vehicle is already assigned to another telemetry unit
  isVehicleTakenByOtherUnit(vehicle: Vehicle, unit: TelemetryUnit): boolean {
    return this.registered_units.some(u => u.id !== unit.id && u.vehicleId === vehicle.Id);
  }

  // Opens or closes the vehicle selection dropdown for an entry
  toggleVehicleDropdown(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.openVehicleDropdownIndex = this.openVehicleDropdownIndex === index ? null : index;
  }

  // Assigns the selected vehicle to the telemetry unit, unless disabled
  selectVehicleForUnit(unit: TelemetryUnit, vehicleId: string, disabled = false): void {
    if (disabled) return;

    unit.vehicleId = vehicleId || undefined;
    this.openVehicleDropdownIndex = null;
  }

  // Saves the changes of the edited entry via the service
  private saveEdit(index: number): void {
    // 1. Determine the unit to save
    const unit = this.pagedUnits[index];
    this.editError = null;

    // 2. Save changes and end edit mode
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

  // Asks for confirmation on the first click and deletes only on the second click
  confirmDelete(index: number, unit: TelemetryUnit): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deleteUnit(unit);
  }

  // Deletes the telemetry unit via the service and updates the local list
  private deleteUnit(unit: TelemetryUnit): void {
    this.editError = null;

    // Trigger the deletion and, on success, clean up the list and open states
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
