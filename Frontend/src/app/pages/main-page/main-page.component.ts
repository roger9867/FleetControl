import { Component, OnInit, AfterViewInit, OnDestroy, ChangeDetectorRef, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { TripsPageComponent } from '../../widgets/fahrten/fahrten.component';
import { TelemetryUnitPageComponent } from '../../widgets/telemetry-units/telemetry-units.component';
import { VehiclesPageComponent } from '../../widgets/vehicles/vehicles.component';
import { PersonPageComponent } from '../../widgets/personen/personen.component';


import { UsbTelemetryService } from '../../services/usb-telemetry.service';


@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TripsPageComponent,
    TelemetryUnitPageComponent,
    VehiclesPageComponent,
    PersonPageComponent
  ],
  templateUrl: './main-page.component.html',
  styleUrl: './main-page.component.scss'
})
export class MainPageComponent implements OnInit, AfterViewInit, OnDestroy {

  @ViewChild('contentEl') contentEl!: ElementRef<HTMLDivElement>;
  @ViewChild('contentInner') contentInnerEl!: ElementRef<HTMLDivElement>;

  contentScale = 1;
  private contentResizeObserver?: ResizeObserver;

  @ViewChild('headerEl') headerEl!: ElementRef<HTMLElement>;
  @ViewChild('appMenuEl') appMenuEl!: ElementRef<HTMLElement>;
  @ViewChild('tabsMeasureEl') tabsMeasureEl!: ElementRef<HTMLElement>;
  @ViewChild('headerRightEl') headerRightEl!: ElementRef<HTMLElement>;

  tabsCollapsed = false;
  tabsDropdownOpen = false;
  private tabsResizeObserver?: ResizeObserver;

/*
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
  */

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

  /*
  sidebarTabs = [
    { icon: 'home', label: 'Aktion 1' },
    { icon: 'lock', label: 'Aktion 2'},
    { icon: 'lock', label: 'Aktion 3' },
    { icon: 'person', label: 'Aktion 4' },
    { icon: 'person', label: 'Aktion 5' }
  ];

  result: any;
  */

  uuids: string[] = [];
  private usbSub?: Subscription;

  constructor(
    private usbTelemetryService: UsbTelemetryService,
    private cdr: ChangeDetectorRef
  ) {
    this.applyTheme();
  }

  ngOnInit() {
    this.usbSub = this.usbTelemetryService.connectedUnitIds$
      .subscribe(uuids => {
        this.uuids = uuids;
        this.cdr.detectChanges();
      });
  }

  ngAfterViewInit() {
    this.contentResizeObserver = new ResizeObserver(() => this.updateContentScale());
    this.contentResizeObserver.observe(this.contentEl.nativeElement);
    this.contentResizeObserver.observe(this.contentInnerEl.nativeElement);
    this.updateContentScale();

    this.tabsResizeObserver = new ResizeObserver(() => this.updateTabsCollapse());
    this.tabsResizeObserver.observe(this.headerEl.nativeElement);
    this.tabsResizeObserver.observe(this.appMenuEl.nativeElement);
    this.tabsResizeObserver.observe(this.tabsMeasureEl.nativeElement);
    this.tabsResizeObserver.observe(this.headerRightEl.nativeElement);
    this.updateTabsCollapse();
  }

  ngOnDestroy() {
    this.usbSub?.unsubscribe();
    this.contentResizeObserver?.disconnect();
    this.tabsResizeObserver?.disconnect();
  }

  @HostListener('document:click')
  onDocumentClickCloseTabsDropdown() {
    this.tabsDropdownOpen = false;
  }

  // Kollabiert die Reiter in ein Dropdown, sobald sie nicht mehr nebeneinander
  // in den Header passen. tabsMeasureEl ist eine unsichtbare Kopie der Reiter,
  // die immer gerendert bleibt, damit die natuerliche Breite auch messbar ist,
  // waehrend gerade das Dropdown angezeigt wird.
  private updateTabsCollapse() {
    const header = this.headerEl?.nativeElement;
    const appMenu = this.appMenuEl?.nativeElement;
    const measure = this.tabsMeasureEl?.nativeElement;
    const headerRight = this.headerRightEl?.nativeElement;
    if (!header || !appMenu || !measure || !headerRight) return;

    const buffer = 32;
    const available = header.clientWidth - appMenu.offsetWidth - headerRight.offsetWidth - buffer;
    const needed = measure.scrollWidth;

    const shouldCollapse = needed > available;
    if (shouldCollapse !== this.tabsCollapsed) {
      this.tabsCollapsed = shouldCollapse;
      if (!shouldCollapse) {
        this.tabsDropdownOpen = false;
      }
      this.cdr.detectChanges();
    }
  }

  // Passt den Inhalt per transform: scale() an den verfuegbaren Platz an,
  // statt Widgets/Tabellen zusammenzuquetschen: scrollWidth ist vom
  // angewendeten transform unabhaengig (reine Layout-Groesse), daher liefert
  // es zuverlaessig die natuerliche Breite bei scale(1).
  private updateContentScale() {
    const container = this.contentEl?.nativeElement;
    const inner = this.contentInnerEl?.nativeElement;
    if (!container || !inner) return;

    const style = getComputedStyle(container);
    const paddingX = parseFloat(style.paddingLeft) + parseFloat(style.paddingRight);
    const availableWidth = container.clientWidth - paddingX;
    const naturalWidth = inner.scrollWidth;

    const nextScale = naturalWidth > availableWidth && availableWidth > 0
      ? availableWidth / naturalWidth
      : 1;

    if (Math.abs(nextScale - this.contentScale) > 0.005) {
      this.contentScale = nextScale;
      this.cdr.detectChanges();
    }
  }

}
