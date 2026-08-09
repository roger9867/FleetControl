import { Component, OnInit, OnDestroy, ElementRef, ViewChild, HostListener, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { CardWidget } from '../card-widget/card-widget.component';
import { TripChart } from '../trip-chart/trip-chart.component';
import { Trip, TripPoint } from '../../models/trip.model';
import { Vehicle } from '../../models/vehicle.model';
import { Person } from '../../models/person.model';
import { TelemetryUnit } from '../../models/telemetry-unit.model';
import { TripService } from '../../services/trip.service';
import { VehicleService } from '../../services/vehicle.service';
import { TelemetryUnitService } from '../../services/telemetry-unit.service';
import { PersonService } from '../../services/person.service';
import { VehicleLiveService, VehiclePositionUpdate, TripEndedUpdate } from '../../services/vehicle-live.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, FormsModule, CardWidget, TripChart],
  templateUrl: './fahrten.component.html',
  styleUrls: ['./fahrten.component.scss']
})
export class LayoutComponent implements OnInit, OnDestroy {

  trips: Trip[] = [];
  totalTripCount = 0;
  vehicles: Vehicle[] = [];
  telemetryUnits: TelemetryUnit[] = [];
  persons: Person[] = [];

  viewMode: 'routen' | 'details' = 'routen';

  // dummyVehicles: Vehicle[] = [
  //   { Id: 'v1', licensePlate: 'FL-1000', brand: 'VW', modelName: 'Transporter', year: 2015, color: 'Weiß', identNr: '100001', requiredLicense: 'C1', powerPs: 140, firstRegistration: '2015-03-10' },
  //   { Id: 'v2', licensePlate: 'FL-1001', brand: 'Mercedes', modelName: 'Sprinter', year: 2018, color: 'Silber', identNr: '100002', requiredLicense: 'C1E', powerPs: 163, firstRegistration: '2018-06-21' },
  //   { Id: 'v3', licensePlate: 'FL-1002', brand: 'BMW', modelName: 'X3', year: 2020, color: 'Schwarz', identNr: '100003', requiredLicense: 'B', powerPs: 190, firstRegistration: '2020-01-15' },
  //   { Id: 'v4', licensePlate: 'FL-1003', brand: 'Audi', modelName: 'A4', year: 2019, color: 'Blau', identNr: '100004', requiredLicense: 'B', powerPs: 150, firstRegistration: '2019-09-05' },
  //   { Id: 'v5', licensePlate: 'FL-1004', brand: 'Ford', modelName: 'Transit', year: 2016, color: 'Rot', identNr: '100005', requiredLicense: 'C1', powerPs: 130, firstRegistration: '2016-11-30' }
  // ];

  // dummyTelemetryUnits: string[] = ['TU-1001', 'TU-1002', 'TU-1003'];

  // dummyPersons: Person[] = [
  //   { Id: 'p1', firstName: 'Anna', lastName: 'Schmidt', birthDate: '1970-05-20' },
  //   { Id: 'p2', firstName: 'Ben', lastName: 'Müller', birthDate: '1971-05-20' },
  //   { Id: 'p3', firstName: 'Clara', lastName: 'Fischer', birthDate: '1972-05-20' },
  //   { Id: 'p4', firstName: 'David', lastName: 'Weber', birthDate: '1973-05-20' },
  //   { Id: 'p5', firstName: 'Emma', lastName: 'Meyer', birthDate: '1974-05-20' }
  // ];

  licenseClasses: string[] = [
    'AM', 'A1', 'A2', 'A', 'B', 'BE',
    'C1', 'C1E', 'C', 'CE', 'D1', 'D1E', 'D', 'DE'
  ];

  showAdvancedVehicleFilter = false;
  advancedVehicleFilter = this.emptyAdvancedVehicleFilter();
  appliedAdvancedVehicleFilter = this.emptyAdvancedVehicleFilter();

  showAdvancedPersonFilter = false;
  advancedPersonFilter = this.emptyAdvancedPersonFilter();
  appliedAdvancedPersonFilter = this.emptyAdvancedPersonFilter();

  sampleIntervals: { label: string; seconds: number }[] = [
    { label: '1/s', seconds: 1 },
    { label: '1/10s', seconds: 10 },
    { label: '1/30s', seconds: 30 },
    { label: '1/min', seconds: 60 },
    { label: '1/2min', seconds: 120 }
  ];

  readonly maxVehicleFilters = 10;
  readonly maxTelemetryUnitFilters = 10;
  readonly maxPersonFilters = 10;

  filterVehicleId = '';
  filterVehicleIds: string[] = [];
  filterTelemetryUnitId = '';
  filterTelemetryUnitIds: string[] = [];
  filterPersonId = '';
  filterPersonIds: string[] = [];
  filterStart = '';
  filterStop = '';
  sampleIntervalSeconds = 30;

  appliedFilterVehicleIds: string[] = [];
  appliedFilterTelemetryUnitIds: string[] = [];
  appliedFilterPersonIds: string[] = [];
  appliedFilterStart = '';
  appliedFilterStop = '';
  appliedSampleIntervalSeconds = 30;

  vehicleSearch = '';
  showVehicleOptions = false;

  telemetrySearch = '';
  showTelemetryOptions = false;

  personSearch = '';
  showPersonOptions = false;

  @ViewChild('vehicleAutocomplete') vehicleAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild('telemetryAutocomplete') telemetryAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild('personAutocomplete') personAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild(CardWidget) cardWidget?: CardWidget;

  selectedIndex: number | null = null;
  deleteConfirmIndex: number | null = null;
  deleteError: string | null = null;

  tripPageSize = 10;
  currentTripPage = 1;

  private liveSubscription?: Subscription;
  private tripEndedSubscription?: Subscription;

  constructor(
    private tripService: TripService,
    private vehicleService: VehicleService,
    private telemetryUnitService: TelemetryUnitService,
    private personService: PersonService,
    private vehicleLiveService: VehicleLiveService,
    private cdr: ChangeDetectorRef
  ) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node;

    if (this.deleteConfirmIndex !== null) {
      const targetEl = target as HTMLElement;
      if (!targetEl.closest || !targetEl.closest('.delete-btn')) {
        this.deleteConfirmIndex = null;
      }
    }

    if (this.vehicleAutocompleteRef && !this.vehicleAutocompleteRef.nativeElement.contains(target)) {
      this.showVehicleOptions = false;
    }

    if (this.telemetryAutocompleteRef && !this.telemetryAutocompleteRef.nativeElement.contains(target)) {
      this.showTelemetryOptions = false;
    }

    if (this.personAutocompleteRef && !this.personAutocompleteRef.nativeElement.contains(target)) {
      this.showPersonOptions = false;
    }
  }

  ngOnInit(): void {
    // this.trips = this.generateDummyTrips().slice(0, this.tripPageSize);
    // this.totalTripCount = this.trips.length;
    // this.vehicles = this.dummyVehicles;
    this.loadTripPage();
    this.loadVehicles();
    this.loadTelemetryUnits();
    this.loadPersons();

    this.liveSubscription = this.vehicleLiveService.vehicleUpdate$.subscribe(update => {
      this.onVehicleUpdate(update);
    });

    this.tripEndedSubscription = this.vehicleLiveService.tripEnded$.subscribe(update => {
      this.onTripEnded(update);
    });
  }

  ngOnDestroy(): void {
    this.liveSubscription?.unsubscribe();
    this.tripEndedSubscription?.unsubscribe();
  }

  // Beendet eine lokal bereits geladene, noch laufende Fahrt sofort, statt
  // erst beim naechsten manuellen Neuladen davon zu erfahren - relevant
  // dafuer, dass der pulsierende letzte Punkt auf der Karte aufhoert zu
  // pulsieren, sobald die Fahrt tatsaechlich (serverseitig) beendet wurde.
  private onTripEnded(update: TripEndedUpdate): void {
    const trip = this.trips.find(t => t.id === update.tripId);
    if (!trip || trip.end) return;

    trip.end = update.endTimestamp;
    this.cdr.detectChanges();
  }

  // Haengt einen Live-Punkt nur an, wenn er zeitlich in eine bereits
  // geladene, laufende Fahrt des betroffenen Fahrzeugs faellt - ohne
  // Fahrtende gilt "laufend bis jetzt", mit Fahrtende wird strikt auf
  // [start, end] geprueft, damit keine verspaeteten Events abgeschlossene
  // Fahrten nachtraeglich verfaelschen.
  private onVehicleUpdate(update: VehiclePositionUpdate): void {
    const pointTime = new Date(update.timestamp).getTime();

    const affectedTrip = this.trips.find(trip => {
      if (trip.vehicleId !== update.vehicleId) return false;

      const start = new Date(trip.start).getTime();
      if (pointTime < start) return false;

      if (trip.end) {
        const end = new Date(trip.end).getTime();
        if (pointTime > end) return false;
      }

      return true;
    });

    if (!affectedTrip) return;

    affectedTrip.points.push({
      lat: update.lat,
      lng: update.lng,
      timestamp: update.timestamp,
      speedKmh: update.speedKmh,
      accelMs2: update.accelMs2
    });

    this.cdr.detectChanges();
  }

  private loadVehicles(): void {
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        this.vehicles = vehicles;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  private loadTelemetryUnits(): void {
    this.telemetryUnitService.getUnits().subscribe({
      next: (units) => {
        this.telemetryUnits = units;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  private loadPersons(): void {
    this.personService.loadAll().subscribe({
      next: (persons) => {
        this.persons = persons;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  private loadTripPage(): void {
    this.tripService.loadPage(this.currentTripPage, this.tripPageSize).subscribe({
      next: (result) => {
        this.trips = result.trips;
        this.totalTripCount = result.totalCount;
        this.selectedIndex = null;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  get filteredTrips(): Trip[] {
    return this.trips.filter(trip => {
      if (this.appliedFilterVehicleIds.length && !this.appliedFilterVehicleIds.includes(trip.vehicleId ?? '')) return false;
      if (this.appliedFilterTelemetryUnitIds.length && !this.appliedFilterTelemetryUnitIds.includes(trip.telemetryUnitId)) return false;
      if (this.appliedFilterPersonIds.length && !this.appliedFilterPersonIds.includes(trip.driverId ?? '')) return false;
      if (this.appliedFilterStart && new Date(trip.start) < new Date(this.appliedFilterStart)) return false;
      if (this.appliedFilterStop && trip.end && new Date(trip.end) > new Date(this.appliedFilterStop)) return false;
      return true;
    });
  }

  get mapTrips(): Trip[] {
    const source = this.selectedTrip ? [this.selectedTrip] : this.pagedTrips;

    return source.map(trip => ({
      ...trip,
      points: this.sampleByInterval(trip.points, this.appliedSampleIntervalSeconds)
    }));
  }

  get totalTripPages(): number {
    return Math.max(1, Math.ceil(this.totalTripCount / this.tripPageSize));
  }

  get tripPageNumbers(): number[] {
    return Array.from({ length: this.totalTripPages }, (_, i) => i + 1);
  }

  get pagedTrips(): Trip[] {
    return this.filteredTrips;
  }

  get selectedTrip(): Trip | null {
    return this.selectedIndex !== null ? this.pagedTrips[this.selectedIndex] ?? null : null;
  }

  get selectedTripChartData(): Trip[] {
    if (!this.selectedTrip) return [];

    return [{
      ...this.selectedTrip,
      points: this.sampleByInterval(this.selectedTrip.points, this.appliedSampleIntervalSeconds)
    }];
  }

  get filteredVehicleOptions(): Vehicle[] {
    const term = this.vehicleSearch.toLowerCase();
    const f = this.appliedAdvancedVehicleFilter;

    return this.vehicles.filter(v => {
      const matchesSearch = !term || (v.licensePlate ?? '').toLowerCase().includes(term);
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

      return matchesSearch && matchesBrand && matchesModel && matchesColor
        && matchesYearFrom && matchesYearTo && matchesIdentNr && matchesLicense
        && matchesPowerFrom && matchesPowerTo && matchesRegFrom && matchesRegTo;
    });
  }

  get filteredTelemetryOptions(): string[] {
    const term = this.telemetrySearch.toLowerCase();
    return this.telemetryUnits.map(u => u.id).filter(id => id.toLowerCase().includes(term));
  }

  get filteredPersonOptions(): Person[] {
    const term = this.personSearch.toLowerCase();
    const f = this.appliedAdvancedPersonFilter;

    return this.persons.filter(p => {
      const fullName = `${p.firstName ?? ''} ${p.lastName ?? ''}`.toLowerCase();
      const matchesSearch = !term || fullName.includes(term) || p.Id.toLowerCase().includes(term);
      const matchesFirstName = !f.firstName || (p.firstName ?? '').toLowerCase().includes(f.firstName.toLowerCase());
      const matchesLastName = !f.lastName || (p.lastName ?? '').toLowerCase().includes(f.lastName.toLowerCase());
      const matchesBirthFrom = !f.birthDateFrom || (p.birthDate ?? '') >= f.birthDateFrom;
      const matchesBirthTo = !f.birthDateTo || (p.birthDate ?? '') <= f.birthDateTo;

      return matchesSearch && matchesFirstName && matchesLastName && matchesBirthFrom && matchesBirthTo;
    });
  }

  onVehicleSearchChange(): void {
    this.showVehicleOptions = true;
    if (!this.vehicleSearch) this.filterVehicleId = '';
  }

  toggleAdvancedVehicleFilter(): void {
    this.showAdvancedVehicleFilter = !this.showAdvancedVehicleFilter;
  }

  applyFilters(): void {
    this.appliedFilterVehicleIds = [...this.filterVehicleIds];
    this.appliedFilterTelemetryUnitIds = [...this.filterTelemetryUnitIds];
    this.appliedFilterPersonIds = [...this.filterPersonIds];
    this.appliedFilterStart = this.filterStart;
    this.appliedFilterStop = this.filterStop;
    this.appliedSampleIntervalSeconds = this.sampleIntervalSeconds;
    this.appliedAdvancedVehicleFilter = { ...this.advancedVehicleFilter };
    this.appliedAdvancedPersonFilter = { ...this.advancedPersonFilter };
    this.selectedIndex = null;
  }

  selectVehicle(vehicle: Vehicle): void {
    this.filterVehicleId = vehicle.licensePlate ?? '';
    this.vehicleSearch = vehicle.licensePlate ?? '';
    this.showVehicleOptions = false;
  }

  addVehicleFilter(): void {
    if (this.filterVehicleId
      && !this.filterVehicleIds.includes(this.filterVehicleId)
      && this.filterVehicleIds.length < this.maxVehicleFilters) {
      this.filterVehicleIds.push(this.filterVehicleId);
    }

    this.filterVehicleId = '';
    this.vehicleSearch = '';
  }

  removeVehicleFilter(vehicleId: string): void {
    this.filterVehicleIds = this.filterVehicleIds.filter(id => id !== vehicleId);
  }

  onTelemetrySearchChange(): void {
    this.showTelemetryOptions = true;
    if (!this.telemetrySearch) this.filterTelemetryUnitId = '';
  }

  selectTelemetryUnit(unitId: string): void {
    this.filterTelemetryUnitId = unitId;
    this.telemetrySearch = unitId;
    this.showTelemetryOptions = false;
  }

  addTelemetryUnitFilter(): void {
    if (this.filterTelemetryUnitId
      && !this.filterTelemetryUnitIds.includes(this.filterTelemetryUnitId)
      && this.filterTelemetryUnitIds.length < this.maxTelemetryUnitFilters) {
      this.filterTelemetryUnitIds.push(this.filterTelemetryUnitId);
    }

    this.filterTelemetryUnitId = '';
    this.telemetrySearch = '';
  }

  removeTelemetryUnitFilter(unitId: string): void {
    this.filterTelemetryUnitIds = this.filterTelemetryUnitIds.filter(id => id !== unitId);
  }

  onPersonSearchChange(): void {
    this.showPersonOptions = true;
    if (!this.personSearch) this.filterPersonId = '';
  }

  toggleAdvancedPersonFilter(): void {
    this.showAdvancedPersonFilter = !this.showAdvancedPersonFilter;
  }

  selectPerson(person: Person): void {
    this.filterPersonId = person.Id;
    this.personSearch = person.Id;
    this.showPersonOptions = false;
  }

  addPersonFilter(): void {
    if (this.filterPersonId
      && !this.filterPersonIds.includes(this.filterPersonId)
      && this.filterPersonIds.length < this.maxPersonFilters) {
      this.filterPersonIds.push(this.filterPersonId);
    }

    this.filterPersonId = '';
    this.personSearch = '';
  }

  removePersonFilter(personId: string): void {
    this.filterPersonIds = this.filterPersonIds.filter(id => id !== personId);
  }

  selectItem(index: number): void {
    this.selectedIndex = this.selectedIndex === index ? null : index;
  }

  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  confirmDelete(index: number, trip: Trip): void {
    if (!trip.end) return;

    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deleteTrip(trip);
  }

  private deleteTrip(trip: Trip): void {
    this.deleteError = null;

    this.tripService.delete(trip.id).subscribe({
      next: () => {
        this.trips = this.trips.filter(t => t !== trip);
        this.totalTripCount = Math.max(0, this.totalTripCount - 1);
        this.deleteConfirmIndex = null;
        this.selectedIndex = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.deleteError = err?.message ?? 'Löschen fehlgeschlagen.';
        this.deleteConfirmIndex = null;
        this.cdr.detectChanges();
      }
    });
  }

  vehicleLabel(vehicleId?: string | null): string {
    if (!vehicleId) return '';
    const vehicle = this.vehicles.find(v => v.Id === vehicleId);
    return vehicle ? `${vehicleId} - ${vehicle.licensePlate}` : vehicleId;
  }

  driverLabel(driverId?: string | null): string {
    if (!driverId) return '';
    const person = this.persons.find(p => p.Id === driverId);
    return person ? `${driverId} - ${person.firstName} ${person.lastName}` : driverId;
  }

  prevTripPage(): void {
    if (this.currentTripPage > 1) {
      this.currentTripPage--;
      this.loadTripPage();
    }
  }

  nextTripPage(): void {
    if (this.currentTripPage < this.totalTripPages) {
      this.currentTripPage++;
      this.loadTripPage();
    }
  }

  goToTripPage(page: number): void {
    this.currentTripPage = page;
    this.loadTripPage();
  }

  setViewMode(mode: 'routen' | 'details'): void {
    this.viewMode = mode;
  }

  recenterMap(): void {
    this.cardWidget?.recenter();
  }

  resetFilters(): void {
    this.filterVehicleId = '';
    this.filterVehicleIds = [];
    this.filterTelemetryUnitId = '';
    this.filterTelemetryUnitIds = [];
    this.filterPersonId = '';
    this.filterPersonIds = [];
    this.filterStart = '';
    this.filterStop = '';
    this.sampleIntervalSeconds = 30;
    this.appliedFilterVehicleIds = [];
    this.appliedFilterTelemetryUnitIds = [];
    this.appliedFilterPersonIds = [];
    this.appliedFilterStart = '';
    this.appliedFilterStop = '';
    this.appliedSampleIntervalSeconds = 30;
    this.vehicleSearch = '';
    this.telemetrySearch = '';
    this.personSearch = '';
    this.showAdvancedVehicleFilter = false;
    this.showAdvancedPersonFilter = false;
    this.advancedVehicleFilter = this.emptyAdvancedVehicleFilter();
    this.appliedAdvancedVehicleFilter = this.emptyAdvancedVehicleFilter();
    this.advancedPersonFilter = this.emptyAdvancedPersonFilter();
    this.appliedAdvancedPersonFilter = this.emptyAdvancedPersonFilter();
  }

  private emptyAdvancedPersonFilter() {
    return {
      firstName: '',
      lastName: '',
      birthDateFrom: '',
      birthDateTo: ''
    };
  }

  private emptyAdvancedVehicleFilter() {
    return {
      brand: '',
      modelName: '',
      color: '',
      yearFrom: null as number | null,
      yearTo: null as number | null,
      identNr: '',
      requiredLicense: '',
      powerPsFrom: null as number | null,
      powerPsTo: null as number | null,
      firstRegistrationFrom: '',
      firstRegistrationTo: ''
    };
  }

  private sampleByInterval(points: TripPoint[], intervalSeconds: number): TripPoint[] {
    if (!points.length) return points;

    const sampled: TripPoint[] = [points[0]];
    let lastTime = new Date(points[0].timestamp).getTime();

    for (let i = 1; i < points.length; i++) {
      const currentTime = new Date(points[i].timestamp).getTime();

      if ((currentTime - lastTime) / 1000 >= intervalSeconds) {
        sampled.push(points[i]);
        lastTime = currentTime;
      }
    }

    return sampled;
  }

  // private generateDummyTrips(): Trip[] {
  //   const baseLat = 50.9271;
  //   const baseLng = 11.5892;
  //   const pointCount = 30;
  //
  //   return Array.from({ length: 40 }, (_, tripIndex) => {
  //     const startTime = new Date(2026, 6, 1 + (tripIndex % 28), 6 + (tripIndex % 12), 0, 0);
  //     const points: TripPoint[] = [];
  //
  //     let lat = baseLat + (tripIndex % 8) * 0.0025;
  //     let lng = baseLng + (tripIndex % 8) * 0.0025;
  //     let previousSpeedKmh = 0;
  //
  //     for (let p = 0; p < pointCount; p++) {
  //       lat += (Math.random() - 0.35) * 0.0012;
  //       lng += (Math.random() - 0.35) * 0.0012;
  //
  //       const speedKmh = Math.max(0, 30 + Math.sin(p / 5 + tripIndex) * 20 + (Math.random() - 0.5) * 6);
  //       const accelMs2 = (speedKmh - previousSpeedKmh) / 3.6 / 30;
  //       previousSpeedKmh = speedKmh;
  //
  //       points.push({
  //         lat,
  //         lng,
  //         timestamp: new Date(startTime.getTime() + p * 30000).toISOString(),
  //         speedKmh,
  //         accelMs2
  //       });
  //     }
  //
  //     return {
  //       id: `Fahrt ${tripIndex + 1}`,
  //       vehicleId: this.dummyVehicles[tripIndex % this.dummyVehicles.length].licensePlate ?? '',
  //       telemetryUnitId: this.dummyTelemetryUnits[tripIndex % this.dummyTelemetryUnits.length],
  //       driverId: this.dummyPersons[tripIndex % this.dummyPersons.length].Id,
  //       start: points[0].timestamp,
  //       end: points[points.length - 1].timestamp,
  //       points
  //     };
  //   });
  // }
}
