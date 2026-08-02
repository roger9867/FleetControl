import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { Person, DrivingLicense } from '../../models/person.model';
import { PersonService } from '../../services/person.service';

@Component({
  selector: 'app-personen',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './personen.component.html',
  styleUrls: ['./personen.component.scss']
})
export class Personen implements OnInit
{
  persons: Person[] = [];

  pageSize = 10;
  currentPage = 1;

  showCreateForm = false;
  newPerson: Person = this.emptyPerson();
  newLicenseClass = '';
  newLicenseDate = '';

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

  dummyVehicles: string[] = ['FL-1000', 'FL-1001', 'FL-1002', 'FL-1003', 'FL-1004'];

  constructor(private personService: PersonService, private cdr: ChangeDetectorRef) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.deleteConfirmIndex !== null) {
      const target = event.target as HTMLElement;
      if (!target.closest || !target.closest('.delete-btn')) {
        this.deleteConfirmIndex = null;
      }
    }
  }

  ngOnInit(): void {
    this.persons = this.generateDummyPersons(24);

    // Once the backend endpoint exists, a successful load replaces the dummy data.
    this.personService.loadAll().subscribe({
      next: (persons) => {
        if (persons?.length) {
          this.persons = persons;
        }
        this.cdr.detectChanges();
      },
      error: () => {
        // No backend yet — keep the dummy persons.
      }
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.persons.length / this.pageSize));
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  get pagedPersons(): Person[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.persons.slice(start, start + this.pageSize);
  }

  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.selectedIndex = null;
    this.editingIndex = null;
    this.deleteConfirmIndex = null;
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
  }

  onActionMouseDown(event: MouseEvent): void {
    // Fires before the currently focused input's blur, so the action
    // triggers on the first click instead of just defocusing the input.
    event.preventDefault();
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
    this.editSnapshot = { ...this.pagedPersons[index], licenses: [...(this.pagedPersons[index].licenses ?? [])] };
    this.editLicenseClass = '';
    this.editLicenseDate = '';
    this.deleteConfirmIndex = null;
  }

  isEditDirty(person: Person): boolean {
    if (!this.editSnapshot) return false;

    return person.firstName !== this.editSnapshot.firstName
      || person.lastName !== this.editSnapshot.lastName
      || person.birthDate !== this.editSnapshot.birthDate
      || (person.assignedVehicleId ?? '') !== (this.editSnapshot.assignedVehicleId ?? '')
      || JSON.stringify(person.licenses ?? []) !== JSON.stringify(this.editSnapshot.licenses ?? []);
  }

  private saveEdit(index: number): void {
    const person = this.pagedPersons[index];

    this.editError = null;

    this.personService.update(person).subscribe({
      next: (updated) => {
        Object.assign(person, updated);
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

  confirmDelete(index: number, person: Person): void {
    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deletePerson(person);
  }

  private deletePerson(person: Person): void {
    this.editError = null;

    this.personService.delete(person.Id).subscribe({
      next: () => {
        this.persons = this.persons.filter(p => p !== person);
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

  onVehicleAssignmentChange(person: Person, value: string): void {
    person.assignedVehicleId = value || null;
  }

  addLicenseToNewPerson(): void {
    if (!this.newLicenseClass || !this.newLicenseDate) return;

    this.newPerson.licenses = this.newPerson.licenses ?? [];
    if (this.newPerson.licenses.some(l => l.licenseClass === this.newLicenseClass)) return;

    this.newPerson.licenses.push({ licenseClass: this.newLicenseClass, obtainedDate: this.newLicenseDate });
    this.newLicenseClass = '';
    this.newLicenseDate = '';
  }

  removeLicenseFromNewPerson(licenseClass: string): void {
    this.newPerson.licenses = (this.newPerson.licenses ?? []).filter(l => l.licenseClass !== licenseClass);
  }

  addLicenseToPerson(person: Person): void {
    if (!this.editLicenseClass || !this.editLicenseDate) return;

    person.licenses = person.licenses ?? [];
    if (person.licenses.some(l => l.licenseClass === this.editLicenseClass)) return;

    person.licenses.push({ licenseClass: this.editLicenseClass, obtainedDate: this.editLicenseDate });
    this.editLicenseClass = '';
    this.editLicenseDate = '';
  }

  removeLicenseFromPerson(person: Person, licenseClass: string): void {
    person.licenses = (person.licenses ?? []).filter(l => l.licenseClass !== licenseClass);
  }

  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) {
      this.newPerson = this.emptyPerson();
      this.newLicenseClass = '';
      this.newLicenseDate = '';
      this.createError = null;
    }
  }

  createPerson(): void {
    if (!this.newPerson.firstName || !this.newPerson.lastName || !this.newPerson.birthDate) return;

    this.createError = null;

    this.personService.save(this.newPerson).subscribe({
      next: (created) => {
        this.persons.unshift(created);
        this.newPerson = this.emptyPerson();
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

  private generateDummyPersons(count: number): Person[] {
    const firstNames = ['Anna', 'Ben', 'Clara', 'David', 'Emma', 'Felix', 'Greta', 'Hannes', 'Ida', 'Jonas'];
    const lastNames = ['Schmidt', 'Müller', 'Fischer', 'Weber', 'Meyer', 'Wagner', 'Becker', 'Schulz', 'Hoffmann', 'Koch'];

    return Array.from({ length: count }, (_, i) => {
      const birthYear = 1970 + (i % 40);
      const licenseCount = 1 + (i % 3);

      const licenses: DrivingLicense[] = Array.from({ length: licenseCount }, (_, l) => ({
        licenseClass: this.licenseClasses[(i + l) % this.licenseClasses.length],
        obtainedDate: `${birthYear + 18 + l}-0${1 + (l % 9)}-15`
      }));

      return {
        Id: `person-${i + 1}`,
        firstName: firstNames[i % firstNames.length],
        lastName: lastNames[i % lastNames.length],
        birthDate: `${birthYear}-05-20`,
        licenses,
        assignedVehicleId: i % 3 === 0 ? this.dummyVehicles[i % this.dummyVehicles.length] : null
      };
    });
  }
}
