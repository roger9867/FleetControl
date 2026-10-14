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

  tabs = [
    'Fahrzeuge', 'Fahrten', 'Personen', 'Datenerfassungseinheiten'
  ];
  activeTab = 'Fahrzeuge';

  pendingTripVehicleId?: string;
  pendingTripPersonId?: string;
  pendingTripTelemetryUnitId?: string;

  // Switches the active tab
  selectTab(tab: string) {
    this.activeTab = tab;
  }

  // Switches to the trips tab and filters it by the selected vehicle
  onShowTripsForVehicle(identNr: string) {
    this.pendingTripVehicleId = identNr;
    this.activeTab = 'Fahrten';
  }

  // Switches to the trips tab and filters it by the selected person.
  onShowTripsForPerson(personId: string) {
    this.pendingTripPersonId = personId;
    this.activeTab = 'Fahrten';
  }

  // Switches to the trips tab and filters it by the selected telemetry unit.
  onShowTripsForTelemetryUnit(unitId: string) {
    this.pendingTripTelemetryUnitId = unitId;
    this.activeTab = 'Fahrten';
  }

  sidebarClosed = true;

  // Expands or collapses the sidebar
  toggleSidebar() {
    this.sidebarClosed = !this.sidebarClosed;
  }

  isDarkMode = localStorage.getItem('theme') !== 'light';

  // Switches between light and dark theme and saves the choice permanently.
  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem('theme', this.isDarkMode ? 'dark' : 'light');
    this.applyTheme();
  }

  // Sets the matching theme class on the document's root element
  private applyTheme() {
    document.documentElement.classList.toggle('theme-light', !this.isDarkMode);
  }

  uuids: string[] = [];
  private usbSub?: Subscription;

  // Initializes the service and applies the saved theme immediately
  constructor(
    private usbTelemetryService: UsbTelemetryService,
    private cdr: ChangeDetectorRef
  ) {
    this.applyTheme();
  }

  // Subscribes to the connected USB device IDs for display
  ngOnInit() {
    this.usbSub = this.usbTelemetryService.connectedUnitIds$
      .subscribe(uuids => {
        this.uuids = uuids;
        this.cdr.detectChanges();
      });
  }

  // Sets up ResizeObserver for content scaling and tab collapsing.
  ngAfterViewInit() {
    // 1. Watch size changes of the content area
    this.contentResizeObserver = new ResizeObserver(() => this.updateContentScale());
    this.contentResizeObserver.observe(this.contentEl.nativeElement);
    this.contentResizeObserver.observe(this.contentInnerEl.nativeElement);
    this.updateContentScale();

    // 2. Watch size changes of the header for the tab layout
    this.tabsResizeObserver = new ResizeObserver(() => this.updateTabsCollapse());
    this.tabsResizeObserver.observe(this.headerEl.nativeElement);
    this.tabsResizeObserver.observe(this.appMenuEl.nativeElement);
    this.tabsResizeObserver.observe(this.tabsMeasureEl.nativeElement);
    this.tabsResizeObserver.observe(this.headerRightEl.nativeElement);
    this.updateTabsCollapse();
  }

  // Ends subscription and observer when the component is destroyed
  ngOnDestroy() {
    this.usbSub?.unsubscribe();
    this.contentResizeObserver?.disconnect();
    this.tabsResizeObserver?.disconnect();
  }

  // Closes the tab dropdown on a click outside
  @HostListener('document:click')
  onDocumentClickCloseTabsDropdown() {
    this.tabsDropdownOpen = false;
  }

  // Collapses the tabs into a dropdown as soon as they no longer fit side by side
  // fit into the header
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

  // Scales the content to the available space via transform: scale(),
  // instead of squeezing widgets/tables
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
