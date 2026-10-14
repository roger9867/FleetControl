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

  // Returns the driver's license classes not yet assigned to the new person
  get availableNewLicenseClasses(): string[] {
    const taken = new Set((this.newPerson.licenses ?? []).map(l => l.licenseClass));
    return this.licenseClasses.filter(c => !taken.has(c));
  }

  // Returns the driver's license classes not yet assigned to the edited person
  availableEditLicenseClasses(person: Person): string[] {
    const taken = new Set((person.licenses ?? []).map(l => l.licenseClass));
    return this.licenseClasses.filter(c => !taken.has(c));
  }

  // Initializes the component with the required services
  constructor(
    private personService: PersonService,
    private vehicleService: VehicleService,
    private telemetryUnitService: TelemetryUnitService,
    private cdr: ChangeDetectorRef
  ) {}

  // Closes the open delete confirmation and vehicle dropdown on a click outside
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    // Close the delete confirmation if the click was outside the delete button
    if (this.deleteConfirmIndex !== null) {
      const target = event.target as HTMLElement;
      if (!target.closest || !target.closest('.delete-btn')) {
        this.deleteConfirmIndex = null;
      }
    }

    // Close the vehicle dropdown if the click was outside the selection
    if (this.showNewVehicleDropdown) {
      const target = event.target as HTMLElement;
      if (!target.closest || !target.closest('.vehicle-select')) {
        this.showNewVehicleDropdown = false;
      }
    }
  }

  // Loads persons, vehicles and telemetry units from the server at startup
  ngOnInit(): void {
    // Load persons
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

    // Load vehicles
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

    // Load telemetry units
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

  // Reloads the vehicle list so that assignedPersonId is up to date after assignment changes
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

  // Links each person to their assigned vehicle based on the vehicle data
  private syncAssignedVehicles(): void {
    for (const person of this.persons) {
      const assignedVehicle = this.vehicles.find(v => v.assignedPersonId === person.Id);
      person.assignedVehicleId = assignedVehicle?.Id ?? null;
    }
  }

  // Checks whether advanced person filters (first/last name, employee number) are set
  private hasActivePersonAdvanced(): boolean {
    const f = this.appliedFilters.personAdvanced;
    return !!(f.firstName || f.lastName || f.employeeNr);
  }

  // Checks whether a person matches the active person filters
  private matchesPersonTier(p: Person): boolean {
    const f = this.appliedFilters.personAdvanced;
    const matchesChip = this.appliedFilters.personIds.length === 0 || this.appliedFilters.personIds.includes(p.Id);
    const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
    const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
    const matchesEmployeeNr = !f.employeeNr || p.Id.toLowerCase().includes(f.employeeNr.toLowerCase());

    return matchesChip && matchesFirstName && matchesLastName && matchesEmployeeNr;
  }

  // Returns the persons matching the currently applied filters (person, vehicle, telemetry level)
  get filteredPersons(): Person[] {
    // Determine active filter levels
    const personTierActive = this.appliedFilters.personIds.length > 0 || this.hasActivePersonAdvanced();
    const vehicleTierActive = this.appliedFilters.vehicleIds.length > 0;
    const telemetryTierActive = this.appliedFilters.telemetryUnitIds.length > 0;

    return this.persons.filter(p => {
      const results: boolean[] = [];

      // Collect matches per active filter level
      if (personTierActive) results.push(this.matchesPersonTier(p));
      if (vehicleTierActive) results.push(this.appliedFilters.vehicleIds.includes(p.assignedVehicleId ?? ''));

      if (telemetryTierActive) {
        const assignedVehicle = this.vehicles.find(v => v.Id === p.assignedVehicleId);
        results.push(this.appliedFilters.telemetryUnitIds.includes(assignedVehicle?.telemetryUnit?.id ?? ''));
      }

      if (results.length === 0) return true;

      // Combine results depending on the mode (union or intersection)
      return this.appliedFilters.mode === 'union' ? results.some(Boolean) : results.every(Boolean);
    });
  }

  // Calculates the total number of pages from the filtered persons
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredPersons.length / this.pageSize));
  }

  // Returns the list of page numbers for the pagination
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  // Returns the persons of the currently displayed page
  get pagedPersons(): Person[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredPersons.slice(start, start + this.pageSize);
  }

  // Takes over newly applied filters and resets the view
  onFiltersApplied(filters: AppliedFilters): void {
    this.appliedFilters = filters;
    this.currentPage = 1;
    this.selectedIndex = null;
  }

  // Switches to the previous page and resets the selection
  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  // Switches to the next page and resets the selection
  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  // Jumps to a specific page and resets the selection
  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  // Marks an entry as selected and ends editing/delete confirmation
  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
    this.editError = null;
    this.deleteConfirmIndex = null;
  }

  // Selects an entry by click, unless it is currently being edited
  onBoxClick(index: number): void {
    if (this.editingIndex === index) return;

    this.selectItem(index);
  }

  // Collapses the selected entry again and resets all related state
  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
    this.editError = null;
    this.editSnapshot = null;
    this.deleteConfirmIndex = null;
  }

  // Prevents the default behavior when pressing the mouse button on action buttons
  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  // Reports the request to show the trips of a person to the parent component
  onShowTrips(person: Person): void {
    this.showTrips.emit(person.Id);
  }

  // Prevents the selection from closing when clicking the action area and closes the delete confirmation if needed
  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  // Toggles the edit mode of an entry or saves the changes
  toggleEdit(index: number): void {
    if (this.editingIndex === index) {
      this.saveEdit(index);
      return;
    }

    // Start edit mode and save the initial state as a snapshot
    this.editingIndex = index;
    this.editError = null;
    this.editSnapshot = { ...this.pagedPersons[index], licenses: [...(this.pagedPersons[index].licenses ?? [])] };
    this.editLicenseClass = '';
    this.editLicenseDate = '';
    this.deleteConfirmIndex = null;
  }

  // Checks whether the edited person differs from the original snapshot
  isEditDirty(person: Person): boolean {
    if (!this.editSnapshot) return false;

    return person.firstName !== this.editSnapshot.firstName
      || person.lastName !== this.editSnapshot.lastName
      || person.birthDate !== this.editSnapshot.birthDate
      || (person.assignedVehicleId ?? '') !== (this.editSnapshot.assignedVehicleId ?? '')
      || JSON.stringify(person.licenses ?? []) !== JSON.stringify(this.editSnapshot.licenses ?? []);
  }

  // Saves the changes of the edited person via the server
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

  // Asks for confirmation on the first click and deletes the person on the second click
  confirmDelete(index: number, person: Person): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deletePerson(person);
  }

  // Deletes the person via the server and removes them from the local list
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

  // Assigns a new vehicle to the person or removes the assignment
  onVehicleAssignmentChange(person: Person, value: string): void {
    person.assignedVehicleId = value || null;
  }

  // Checks whether a vehicle is already assigned to another person
  isVehicleTakenByOtherPerson(vehicle: Vehicle, person: Person): boolean {
    return !!vehicle.assignedPersonId && vehicle.assignedPersonId !== person.Id;
  }

  // Builds the display label for a vehicle from its id
  vehicleLabel(vehicleId: string | null | undefined): string {
    if (!vehicleId) return 'Kein Fahrzeug';

    const vehicle = this.vehicles.find(v => v.Id === vehicleId);
    return vehicle ? `${vehicle.Id} — ${vehicle.brand ?? ''} ${vehicle.modelName ?? ''}`.trim() : vehicleId;
  }

  // Opens or closes the vehicle selection dropdown for the new person
  toggleNewVehicleDropdown(): void {
    this.showNewVehicleDropdown = !this.showNewVehicleDropdown;
  }

  // Selects a vehicle for the new person, unless it is locked
  selectNewPersonVehicle(vehicleId: string | null, disabled = false): void {
    if (disabled) return;

    this.newPerson.assignedVehicleId = vehicleId;
    this.showNewVehicleDropdown = false;
  }

  // Adds a driver's license to the new person if class and date are valid
  addLicenseToNewPerson(): void {
    if (!this.newLicenseClass || !this.newLicenseDate) return;

    this.newPerson.licenses = this.newPerson.licenses ?? [];
    if (this.newPerson.licenses.some(l => l.licenseClass === this.newLicenseClass)) return;

    this.newPerson.licenses.push({ licenseClass: this.newLicenseClass, obtainedDate: this.newLicenseDate });
    this.newLicenseClass = '';
    this.newLicenseDate = '';
  }

  // Removes a driver's license from the new person
  removeLicenseFromNewPerson(licenseClass: string): void {
    this.newPerson.licenses = (this.newPerson.licenses ?? []).filter(l => l.licenseClass !== licenseClass);
  }

  // Adds a driver's license to an existing person if class and date are valid
  addLicenseToPerson(person: Person): void {
    if (!this.editLicenseClass || !this.editLicenseDate) return;

    person.licenses = person.licenses ?? [];
    if (person.licenses.some(l => l.licenseClass === this.editLicenseClass)) return;

    person.licenses.push({ licenseClass: this.editLicenseClass, obtainedDate: this.editLicenseDate });
    this.editLicenseClass = '';
    this.editLicenseDate = '';
  }

  // Removes a driver's license from an existing person
  removeLicenseFromPerson(person: Person, licenseClass: string): void {
    person.licenses = (person.licenses ?? []).filter(l => l.licenseClass !== licenseClass);
  }

  // Opens or closes the form for creating a new person
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

  // Creates a new person via the server if the required fields are filled in
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

  // Creates an empty person as a template for the create form
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
