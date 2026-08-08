import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

// import { CardWidget } from '../../widgets/card-widget/card-widget.component';
import { LayoutComponent } from '../../widgets/fahrten/fahrten.component';
import { TelemetryUnits } from '../../widgets/telemetry-units/telemetry-units.component';
import { Vehicles } from '../../widgets/vehicles/vehicles.component';
import { Personen } from '../../widgets/personen/personen.component';


import { TelemetryUnitService } from '../../services/telemetry-unit.service';


@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    // CardWidget,
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
    // 'Statistik',
    'Fahrzeuge', 'Fahrten', 'Personen', 'Datenerfassungseinheiten'
  ];
  activeTab = 'Fahrzeuge';

  selectTab(tab: string) {
    this.activeTab = tab;
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
  private broadcastSub?: Subscription;

  constructor(
    private telemetryUnitService: TelemetryUnitService,
    private cdr: ChangeDetectorRef
  ) {
    this.applyTheme();
  }

  ngOnInit() {
    this.broadcastSub = this.telemetryUnitService.pollConnectedUnits()
      .subscribe(uuids => {
        this.uuids = uuids;
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy() {
    this.broadcastSub?.unsubscribe();
  }

}
