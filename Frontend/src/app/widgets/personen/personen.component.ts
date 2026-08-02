import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { Person, DrivingLicense } from '../../models/person.model';

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
  editLicenseClass = '';
  editLicenseDate = '';

  licenseClasses: string[] = [
    'AM', 'A1', 'A2', 'A', 'B', 'BE',
    'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE'
  ];

  dummyVehicles: string[] = ['FL-1000', 'FL-1001', 'FL-1002', 'FL-1003', 'FL-1004'];

  ngOnInit(): void {
    this.persons = this.generateDummyPersons(24);
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

  selectItem(index: number): void {
    this.selectedIndex = index;
    this.editingIndex = null;
  }

  collapseItem(): void {
    this.selectedIndex = null;
    this.editingIndex = null;
  }

  toggleEdit(index: number): void {
    this.editingIndex = this.editingIndex === index ? null : index;
    this.editLicenseClass = '';
    this.editLicenseDate = '';
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
    }
  }

  createPerson(): void {
    if (!this.newPerson.firstName || !this.newPerson.lastName) return;

    this.persons.unshift({
      Id: crypto.randomUUID(),
      firstName: this.newPerson.firstName,
      lastName: this.newPerson.lastName,
      employeeNr: this.newPerson.employeeNr,
      birthDate: this.newPerson.birthDate,
      licenses: this.newPerson.licenses ?? [],
      assignedVehicleId: this.newPerson.assignedVehicleId ?? null
    });

    this.newPerson = this.emptyPerson();
    this.showCreateForm = false;
    this.currentPage = 1;
  }

  private emptyPerson(): Person {
    return {
      Id: '',
      firstName: '',
      lastName: '',
      employeeNr: '',
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
        employeeNr: `MA-${1000 + i}`,
        birthDate: `${birthYear}-05-20`,
        licenses,
        assignedVehicleId: i % 3 === 0 ? this.dummyVehicles[i % this.dummyVehicles.length] : null
      };
    });
  }
}
