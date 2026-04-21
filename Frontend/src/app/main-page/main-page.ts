import { Component, AfterViewInit, AfterViewChecked, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { CardWidget } from '../widgets/card-widget/card-widget';
import { LayoutComponent } from '../widgets/fahrten/fahrten.component';



@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [CommonModule, FormsModule, CardWidget, LayoutComponent],
  templateUrl: './main-page.html',
  styleUrl: './main-page.scss'
})
export class MainPageComponent {

person = {
    firstName: '',
    lastName: '',
    birthDate: '',
    birthPlace: '',
    email: '',
    address: '',
    isAdmin: false
  };

  submitPerson() {
    console.log('Person gespeichert:', this.person);

    this.person = {
      firstName: '',
      lastName: '',
      birthDate: '',
      birthPlace: '',
      email: '',
      address: '',
      isAdmin: false
    };
  }

  tabs = ['Statistik', 'Fahrten', 'Fahrzeuge',  'Datenerfassunseinheiten', 'Personen'];
  activeTab = 'Fahrten';

  selectTab(tab: string) {
    this.activeTab = tab;
  }

  sidebarClosed = false;

  toggleSidebar() {
    this.sidebarClosed = !this.sidebarClosed;
  }

  sidebarTabs = [
    { icon: 'home', label: 'Aktion 1' },
    { icon: 'lock', label: 'Aktion 2'},
    { icon: 'lock', label: 'Aktion 3' },
    { icon: 'person', label: 'Aktion 4' },
    { icon: 'person', label: 'Aktion 5' }
  ];

}
