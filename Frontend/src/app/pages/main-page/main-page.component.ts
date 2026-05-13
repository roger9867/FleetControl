import { Component, AfterViewInit, AfterViewChecked, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { CardWidget } from '../../widgets/card-widget/card-widget.component';
import { LayoutComponent } from '../../widgets/fahrten/fahrten.component';
import { TelemetryUnits } from '../../widgets/telemetry-units/telemetry-units.component';
import { Vehicles } from '../../widgets/vehicles/vehicles.component';


import { TelemetryUnitService } from '../../services/telemetry-unit.service';


@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardWidget,
    LayoutComponent,
    TelemetryUnits,
    Vehicles
  ],
  templateUrl: './main-page.component.html',
  styleUrl: './main-page.component.scss'
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

  tabs = ['Statistik', 'Fahrten', 'Fahrzeuge',  'Datenerfassungseinheiten', 'Personen'];
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




  result: any;

  constructor(private telemetryUnitService: TelemetryUnitService) {}

  ngOnInit() {
    this.sendBroadcast();
  }

  uuids: string[] = [];

  sendBroadcast() {
  console.log('SEND START');

  this.telemetryUnitService.broadcastCommand()
    .subscribe(res => {

      console.log('RAW RESPONSE:', res);

      const values = Object.values(res ?? {}).filter(Boolean);

      console.log('EXTRACTED:', values);

      this.uuids = values as string[];

      console.log('FINAL UUIDS:', this.uuids);
    });
}

}
