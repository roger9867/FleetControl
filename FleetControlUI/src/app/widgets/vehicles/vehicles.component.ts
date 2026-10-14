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

  // Closes open dropdowns/delete confirmation when clicking outside
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node;

    // Close the delete confirmation if the click was not on the delete button
    if (this.deleteConfirmIndex !== null) {
      const targetEl = target as HTMLElement;
      if (!targetEl.closest || !targetEl.closest('.delete-btn')) {
        this.deleteConfirmIndex = null;
      }
    }

    // Close the telemetry dropdown if the click was outside of it
    if (this.openTelemetryDropdownIndex !== null) {
      const targetEl = target as HTMLElement;
      if (!targetEl.closest || !targetEl.closest('.unit-select')) {
        this.openTelemetryDropdownIndex = null;
      }
    }

    // Close the driver dropdown if the click was outside of it
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

  // Loads all vehicles, telemetry units and persons at startup and subscribes to live updates
  ngOnInit(): void {
    // 1. Load vehicles
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

    // 2. Load telemetry units
    this.telemetryUnitService.getUnits().subscribe({
      next: (units) => {
        if (units?.length) {
          this.telemetryUnits = units;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // 3. Load persons
    this.personService.loadAll().subscribe({
      next: (persons) => {
        if (persons?.length) {
          this.persons = persons;
        }
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    // 4. Subscribe to live position updates
    this.liveSubscription = this.vehicleLiveService.vehicleUpdate$.subscribe(update => {
      this.onVehicleUpdate(update);
    });
  }

  // Ends the live subscription and cleans up running timers
  ngOnDestroy(): void {
    this.liveSubscription?.unsubscribe();
    this.movingTimers.forEach(timer => clearTimeout(timer));
  }

  // Processes a live position update and marks the vehicle as "moving"
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

    // Mark the vehicle as moving
    this.movingVehicleIds.add(update.vehicleId);

    // Reset the existing timeout for this vehicle
    const existingTimer = this.movingTimers.get(update.vehicleId);
    if (existingTimer) {
      clearTimeout(existingTimer);
    }

    // Mark the movement as ended once the timeout expires
    this.movingTimers.set(update.vehicleId, setTimeout(() => {
      this.movingVehicleIds.delete(update.vehicleId);
      this.movingTimers.delete(update.vehicleId);
      this.cdr.detectChanges();
    }, MOVING_TIMEOUT_MS));

    this.cdr.detectChanges();
  }

  // Reloads the current positions of all vehicles
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

  // Returns the IDs of all known telemetry units
  get telemetryUnitIds(): string[] {
    return this.telemetryUnits.map(u => u.id);
  }

  // Checks whether a telemetry unit is already assigned to another vehicle
  isUnitTakenByOtherVehicle(unit: TelemetryUnit, vehicle: Vehicle): boolean {
    return !!unit.vehicleId && unit.vehicleId !== vehicle.Id;
  }

  // Opens or closes the telemetry dropdown for the given row
  toggleTelemetryDropdown(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.openTelemetryDropdownIndex = this.openTelemetryDropdownIndex === index ? null : index;
  }

  // Selects a telemetry unit for the vehicle and closes the dropdown
  selectTelemetryUnit(vehicle: Vehicle, unitId: string, disabled = false): void {
    if (disabled) return;

    this.onTelemetryChange(vehicle, unitId);
    this.openTelemetryDropdownIndex = null;
  }

  // Reloads the telemetry units from the server
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

  // Filters the person list by the advanced driver filter criteria
  get filteredDriverOptions(): Person[] {
    const f = this.advancedDriverFilter;

    return this.persons.filter(p => {
      const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
      const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
      const matchesEmployeeNr = !f.employeeNr || p.Id.toLowerCase().includes(f.employeeNr.toLowerCase());

      return matchesFirstName && matchesLastName && matchesEmployeeNr;
    });
  }

  // Creates the tooltip label (name + ID) for a driver
  private getDriverTooltipLabel(driverId?: string | null): string | undefined {
    if (!driverId) return undefined;

    const person = this.persons.find(p => p.Id === driverId);
    const name = person ? `${person.firstName ?? ''} ${person.lastName ?? ''}`.trim() : '';

    return name
      ? `${name} - ${driverId}`
      : driverId;
  }

  // Checks whether a person is already assigned to another vehicle as driver
  isPersonTakenByOtherVehicle(person: Person, vehicle: Vehicle): boolean {
    return this.vehicles.some(v => v.Id !== vehicle.Id && v.assignedPersonId === person.Id);
  }

  // Returns the display name of the driver assigned to the vehicle
  getDriverLabel(vehicle: Vehicle): string {
    if (!vehicle.assignedPersonId) return 'keinen';

    const person = this.persons.find(p => p.Id === vehicle.assignedPersonId);
    if (!person) return vehicle.assignedPersonId;

    return `${person.firstName ?? ''} ${person.lastName ?? ''}`.trim() || vehicle.assignedPersonId;
  }

  // Opens or closes the driver dropdown for the given row
  toggleDriverDropdown(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.openDriverDropdownIndex = this.openDriverDropdownIndex === index ? null : index;
  }

  // Shows or hides the advanced driver filter form
  toggleAdvancedDriverFilter(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.showAdvancedDriverFilter = !this.showAdvancedDriverFilter;
    if (this.showAdvancedDriverFilter) {
      this.openDriverDropdownIndex = index;
    }
  }

  // Assigns the selected driver to the vehicle and closes the dropdown
  selectDriver(vehicle: Vehicle, personId: string, disabled = false): void {
    if (disabled) return;

    vehicle.assignedPersonId = personId || null;
    this.openDriverDropdownIndex = null;
  }

  // Checks whether at least one advanced vehicle filter criterion is actively set
  private hasActiveVehicleAdvanced(): boolean {
    const f = this.appliedFilters.vehicleAdvanced;
    return !!(f.brand || f.modelName || f.color || f.identNr || f.requiredLicense
      || f.yearFrom != null || f.yearTo != null || f.powerPsFrom != null || f.powerPsTo != null
      || f.firstRegistrationFrom || f.firstRegistrationTo);
  }

  // Checks whether a vehicle matches all active criteria of the vehicle filter level
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

  // Applies all active filter levels (vehicle, telemetry, person) to the vehicle list
  get filteredVehicles(): Vehicle[] {
    // Determine active filter levels
    const vehicleTierActive = this.appliedFilters.vehicleIds.length > 0 || this.hasActiveVehicleAdvanced();
    const telemetryTierActive = this.appliedFilters.telemetryUnitIds.length > 0;
    const personTierActive = this.appliedFilters.personIds.length > 0;

    return this.vehicles.filter(v => {
      // Collect matches per active level
      const results: boolean[] = [];

      if (vehicleTierActive) results.push(this.matchesVehicleTier(v));
      if (telemetryTierActive) results.push(this.appliedFilters.telemetryUnitIds.includes(v.telemetryUnit?.id ?? ''));
      if (personTierActive) results.push(this.appliedFilters.personIds.includes(v.assignedPersonId ?? ''));

      if (results.length === 0) return true;

      // Combine results depending on the mode (AND/OR)
      return this.appliedFilters.mode === 'union' ? results.some(Boolean) : results.every(Boolean);
    });
  }

  // Calculates the total number of pages from the filtered vehicles
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredVehicles.length / this.pageSize));
  }

  // Returns the list of page numbers to display for the pagination
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  // Cuts the current page out of the filtered vehicle list
  get pagedVehicles(): Vehicle[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredVehicles.slice(start, start + this.pageSize);
  }

  // Creates the map points for the currently displayed or selected vehicles
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

  // Re-centers the map on the displayed vehicles
  recenterMap(): void {
    this.vehicleMap?.recenter();
  }

  // Takes over the filters applied in the filter sidebar and resets the page
  onFiltersApplied(filters: AppliedFilters): void {
    this.appliedFilters = filters;
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  // Switches to the previous page
  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Switches to the next page
  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Jumps directly to a specific page
  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  // Selects a row and expands it, discarding any edit state
  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
    this.editError = null;
    this.deleteConfirmIndex = null;
  }

  // Collapses the currently selected/edited row again and resets the related state
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

  // Prevents a click on the action column from changing the focus/selection
  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  // Notifies the parent container that the trips of the vehicle should be shown
  onShowTrips(vehicle: Vehicle): void {
    this.showTrips.emit(vehicle.identNr ?? vehicle.Id);
  }

  // Prevents open elements from closing on clicks inside the action column
  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  // Toggles the edit mode of a row, or saves when called again
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

  // Checks whether the vehicle has changed compared to the edit snapshot
  isEditDirty(vehicle: Vehicle): boolean {
    if (!this.editSnapshot) return false;

    return vehicle.licensePlate !== this.editSnapshot.licensePlate
      || vehicle.firstRegistration !== this.editSnapshot.firstRegistration
      || (vehicle.telemetryUnit?.id ?? '') !== (this.editSnapshot.telemetryUnit?.id ?? '')
      || (vehicle.assignedPersonId ?? '') !== (this.editSnapshot.assignedPersonId ?? '');
  }

  // Saves the changes of the edited row via the service
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

  // Asks for confirmation on the first click and deletes the vehicle on the second click
  confirmDelete(index: number, vehicle: Vehicle): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deleteVehicle(vehicle);
  }

  // Deletes the vehicle via the service and releases the associated telemetry units
  private deleteVehicle(vehicle: Vehicle): void {
    this.editError = null;

    this.vehicleService.delete(vehicle.Id).subscribe({
      next: () => {
        // Remove the vehicle from the local list
        this.vehicles = this.vehicles.filter(v => v !== vehicle);

        // Mark the telemetry unit of the deleted vehicle as free
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

  // Sets the assigned telemetry unit of the vehicle
  onTelemetryChange(vehicle: Vehicle, value: string): void {
    vehicle.telemetryUnit = value ? { id: value } : null;
  }

  // Opens or closes the form for creating a new vehicle and resets it
  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) {
      this.newVehicle = this.emptyVehicle();
      this.createError = null;
      this.identNrError = null;
      this.yearError = null;
    }
  }

  // Formats the license plate of the new vehicle (upper case, max. 10 characters)
  onNewLicensePlateChange(value: string): void {
    this.newVehicle.licensePlate = (value ?? '').toUpperCase().slice(0, 10);
  }

  // Formats the license plate of an existing vehicle (upper case, max. 10 characters)
  onVehicleLicensePlateChange(vehicle: Vehicle, value: string): void {
    vehicle.licensePlate = (value ?? '').toUpperCase().slice(0, 10);
  }

  // Prevents further digit input once the maximum number of digits is reached
  onDigitFieldKeydown(event: KeyboardEvent, currentValue: number | null | undefined, maxDigits = 4): void {
    if (!/^[0-9]$/.test(event.key)) return;

    const currentLength = currentValue != null ? currentValue.toString().length : 0;
    if (currentLength >= maxDigits) {
      event.preventDefault();
    }
  }

  // Prevents negative power values and limits the number of digits of the power (hp)
  onPowerPsKeydown(event: KeyboardEvent): void {
    if (event.key === '-') {
      event.preventDefault();
      return;
    }

    this.onDigitFieldKeydown(event, this.newVehicle.powerPs);
  }

  // Takes over the entered model year and validates it
  onYearChange(value: number | null): void {
    this.newVehicle.year = value ?? undefined;
    this.yearError = this.validateYear(value);
  }

  // Checks whether the model year is within the valid range (from 1981)
  private validateYear(value: number | null | undefined): string | null {
    if (value == null) return null;

    return value >= 1981 ? null : 'Baujahr muss 1981 oder später sein.';
  }

  // Cleans up the entered VIN (upper case, no spaces/separators) and validates it
  onIdentNrChange(value: string): void {
    const sanitized = (value ?? '').toUpperCase().replace(/[\s-]/g, '');
    this.newVehicle.identNr = sanitized;
    this.identNrError = this.validateIdentNr(sanitized);
  }

  // Checks the VIN for length (17 characters) and forbidden characters (I, O, Q)
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

  // Validates the new vehicle and creates it via the service
  createVehicle(): void {
    // Check required fields
    if (!this.newVehicle.modelName || !this.newVehicle.identNr || !this.newVehicle.brand
      || !this.newVehicle.year || !this.newVehicle.requiredLicense
      || !this.newVehicle.powerPs || !this.newVehicle.color) return;

    // Validate VIN
    this.identNrError = this.validateIdentNr(this.newVehicle.identNr);
    if (this.identNrError) {
      this.createError = this.identNrError;
      return;
    }

    // Validate model year
    this.yearError = this.validateYear(this.newVehicle.year);
    if (this.yearError) {
      this.createError = this.yearError;
      return;
    }

    this.createError = null;

    // Save vehicle
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

  // Creates an empty vehicle object as the initial state for the create form
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
