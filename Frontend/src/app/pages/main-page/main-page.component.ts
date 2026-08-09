import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { LayoutComponent } from '../../widgets/fahrten/fahrten.component';
import { TelemetryUnits } from '../../widgets/telemetry-units/telemetry-units.component';
import { Vehicles } from '../../widgets/vehicles/vehicles.component';
import { Personen } from '../../widgets/personen/personen.component';


import { VehicleLiveService } from '../../services/vehicle-live.service';


@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayoutComponent,
    TelemetryUnits,
    Vehicles,
    Personen
  ],
  templateUrl: './main-page.component.html',
  styleUrl: './main-page.component.scss'
})
export class MainPageComponent implements OnInit, OnDestroy {

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

  tabs = [
    'Fahrzeuge', 'Fahrten', 'Personen', 'Datenerfassungseinheiten'
  ];
  activeTab = 'Fahrzeuge';

  pendingTripVehicleId?: string;
  pendingTripPersonId?: string;
  pendingTripTelemetryUnitId?: string;

  selectTab(tab: string) {
    this.activeTab = tab;
  }

  onShowTripsForVehicle(identNr: string) {
    this.pendingTripVehicleId = identNr;
    this.activeTab = 'Fahrten';
  }

  onShowTripsForPerson(personId: string) {
    this.pendingTripPersonId = personId;
    this.activeTab = 'Fahrten';
  }

  onShowTripsForTelemetryUnit(unitId: string) {
    this.pendingTripTelemetryUnitId = unitId;
    this.activeTab = 'Fahrten';
  }

  sidebarClosed = false;

  toggleSidebar() {
    this.sidebarClosed = !this.sidebarClosed;
  }

  isDarkMode = localStorage.getItem('theme') !== 'light';

  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem('theme', this.isDarkMode ? 'dark' : 'light');
    this.applyTheme();
  }

  private applyTheme() {
    document.documentElement.classList.toggle('theme-light', !this.isDarkMode);
  }

  sidebarTabs = [
    { icon: 'home', label: 'Aktion 1' },
    { icon: 'lock', label: 'Aktion 2'},
    { icon: 'lock', label: 'Aktion 3' },
    { icon: 'person', label: 'Aktion 4' },
    { icon: 'person', label: 'Aktion 5' }
  ];




  result: any;

  uuids: string[] = [];
  private usbSub?: Subscription;

  constructor(
    private vehicleLiveService: VehicleLiveService,
    private cdr: ChangeDetectorRef
  ) {
    this.applyTheme();
  }

  ngOnInit() {
    this.usbSub = this.vehicleLiveService.usbUnitsChanged$
      .subscribe(uuids => {
        this.uuids = uuids;
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy() {
    this.usbSub?.unsubscribe();
  }

}
