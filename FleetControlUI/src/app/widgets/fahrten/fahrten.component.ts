import { Component, OnInit, OnDestroy, ElementRef, ViewChild, HostListener, ChangeDetectorRef, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { CardWidgetComponent } from '../card-widget/card-widget.component';
import { TripChartComponent } from '../trip-chart/trip-chart.component';
import { Trip, TripPoint } from '../../models/trip.model';
import { Vehicle } from '../../models/vehicle.model';
import { Person } from '../../models/person.model';
import { TelemetryUnit } from '../../models/telemetry-unit.model';
import { TripService } from '../../services/trip.service';
import { VehicleService } from '../../services/vehicle.service';
import { TelemetryUnitService } from '../../services/telemetry-unit.service';
import { PersonService } from '../../services/person.service';
import { VehicleLiveService, VehiclePositionUpdate, TripEndedUpdate, TripStartedUpdate } from '../../services/vehicle-live.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, FormsModule, CardWidgetComponent, TripChartComponent],
  templateUrl: './fahrten.component.html',
  styleUrls: ['./fahrten.component.scss']
})
export class TripsPageComponent implements OnInit, OnDestroy {

  @Input() presetVehicleId?: string;
  @Output() presetVehicleIdChange = new EventEmitter<string | undefined>();

  @Input() presetPersonId?: string;
  @Output() presetPersonIdChange = new EventEmitter<string | undefined>();

  @Input() presetTelemetryUnitId?: string;
  @Output() presetTelemetryUnitIdChange = new EventEmitter<string | undefined>();

  trips: Trip[] = [];
  totalTripCount = 0;
  vehicles: Vehicle[] = [];
  telemetryUnits: TelemetryUnit[] = [];
  persons: Person[] = [];

  viewMode: 'routen' | 'details' = 'routen';

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
  sampleIntervalSeconds = 10;

  appliedFilterVehicleIds: string[] = [];
  appliedFilterTelemetryUnitIds: string[] = [];
  appliedFilterPersonIds: string[] = [];
  appliedFilterStart = '';
  appliedFilterStop = '';
  appliedSampleIntervalSeconds = 10;

  vehicleSearch = '';
  showVehicleOptions = false;

  telemetrySearch = '';
  showTelemetryOptions = false;

  personSearch = '';
  showPersonOptions = false;

  @ViewChild('vehicleAutocomplete') vehicleAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild('telemetryAutocomplete') telemetryAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild('personAutocomplete') personAutocompleteRef?: ElementRef<HTMLElement>;
  @ViewChild(CardWidgetComponent) cardWidget?: CardWidgetComponent;

  selectedIndex: number | null = null;
  deleteConfirmIndex: number | null = null;
  deleteError: string | null = null;

  tripPageSize = 10;
  currentTripPage = 1;

  private liveSubscription?: Subscription;
  private tripEndedSubscription?: Subscription;
  private tripStartedSubscription?: Subscription;

  // Konstruktor: injiziert die benötigten Services
  constructor(
    private tripService: TripService,
    private vehicleService: VehicleService,
    private telemetryUnitService: TelemetryUnitService,
    private personService: PersonService,
    private vehicleLiveService: VehicleLiveService,
    private cdr: ChangeDetectorRef
  ) {}

  // Schließt offene Dropdowns/Bestätigungen, wenn außerhalb geklickt wird
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

  // Lifecycle-Hook: lädt Anfangsdaten, übernimmt Preset-Filter und abonniert Live-Updates
  ngOnInit(): void {
    // 1. Fahrten und Stammdaten laden
    this.loadTripPage();
    this.loadVehicles();
    this.loadTelemetryUnits();
    this.loadPersons();

    // 2. über Inputs vorgegebene Filter übernehmen und danach zurücksetzen
    if (this.presetVehicleId) {
      this.filterVehicleIds = [this.presetVehicleId];
      this.appliedFilterVehicleIds = [this.presetVehicleId];
      this.presetVehicleIdChange.emit(undefined);
    }

    if (this.presetPersonId) {
      this.filterPersonIds = [this.presetPersonId];
      this.appliedFilterPersonIds = [this.presetPersonId];
      this.presetPersonIdChange.emit(undefined);
    }

    if (this.presetTelemetryUnitId) {
      this.filterTelemetryUnitIds = [this.presetTelemetryUnitId];
      this.appliedFilterTelemetryUnitIds = [this.presetTelemetryUnitId];
      this.presetTelemetryUnitIdChange.emit(undefined);
    }

    // 3. Live-Updates für Fahrzeugposition, Fahrtende und Fahrtstart abonnieren
    this.liveSubscription = this.vehicleLiveService.vehicleUpdate$.subscribe(update => {
      this.onVehicleUpdate(update);
    });

    this.tripEndedSubscription = this.vehicleLiveService.tripEnded$.subscribe(update => {
      this.onTripEnded(update);
    });

    this.tripStartedSubscription = this.vehicleLiveService.tripStarted$.subscribe(update => {
      this.onTripStarted(update);
    });
  }

  ngOnDestroy(): void {
    this.liveSubscription?.unsubscribe();
    this.tripEndedSubscription?.unsubscribe();
    this.tripStartedSubscription?.unsubscribe();
  }

  // Fügt eine neu gestartete Fahrt oben in die Liste ein (nur auf der ersten Seite)
  private onTripStarted(update: TripStartedUpdate): void {
    if (this.currentTripPage !== 1 || this.trips.some(t => t.id === update.tripId)) return;

    this.trips = [{
      id: update.tripId,
      vehicleId: update.vehicleId,
      telemetryUnitId: update.telemetryUnitId,
      driverId: update.driverId,
      start: update.startTimestamp,
      end: null,
      points: []
    }, ...this.trips];

    this.totalTripCount++;
    this.cdr.detectChanges();
  }

  // Trägt den Endzeitpunkt einer beendeten Fahrt in die passende lokale Fahrt ein
  private onTripEnded(update: TripEndedUpdate): void {
    const trip = this.trips.find(t => t.id === update.tripId);
    if (!trip || trip.end) return;

    trip.end = update.endTimestamp;
    this.cdr.detectChanges();
  }

  // Ordnet ein eintreffendes Positions-Update der passenden laufenden Fahrt zu und hängt den Punkt an
  private onVehicleUpdate(update: VehiclePositionUpdate): void {
    const pointTime = new Date(update.timestamp).getTime();

    // 1. betroffene Fahrt anhand Fahrzeug und Zeitraum finden
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

    // 2. neuen Telemetriepunkt an die Fahrt anhängen
    affectedTrip.points.push({
      lat: update.lat,
      lng: update.lng,
      timestamp: update.timestamp,
      speedKmh: update.speedKmh,
      accelMs2: update.accelMs2
    });

    this.cdr.detectChanges();
  }

  // Lädt alle Fahrzeuge für die Filter- und Anzeigelisten
  private loadVehicles(): void {
    this.vehicleService.loadAll().subscribe({
      next: (vehicles) => {
        this.vehicles = vehicles;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Lädt alle Telemetrie-Einheiten für die Filterliste
  private loadTelemetryUnits(): void {
    this.telemetryUnitService.getUnits().subscribe({
      next: (units) => {
        this.telemetryUnits = units;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Lädt alle Personen (Fahrer) für die Filterliste
  private loadPersons(): void {
    this.personService.loadAll().subscribe({
      next: (persons) => {
        this.persons = persons;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  // Lädt die aktuelle Seite der Fahrtenliste vom Server
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

  // Wendet die aktuell angewendeten Filter (Fahrzeug, Einheit, Person, Zeitraum) auf die Fahrten an
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

  // Liefert die für die Kartendarstellung relevanten Fahrten mit reduzierter Punktdichte
  get mapTrips(): Trip[] {
    const source = this.selectedTrip ? [this.selectedTrip] : this.pagedTrips;

    return source.map(trip => ({
      ...trip,
      points: this.sampleByInterval(trip.points, this.appliedSampleIntervalSeconds)
    }));
  }

  // Berechnet die Gesamtanzahl der Seiten anhand von Fahrtenanzahl und Seitengröße
  get totalTripPages(): number {
    return Math.max(1, Math.ceil(this.totalTripCount / this.tripPageSize));
  }

  // Liefert die Liste der Seitenzahlen für die Pagination
  get tripPageNumbers(): number[] {
    return Array.from({ length: this.totalTripPages }, (_, i) => i + 1);
  }

  // Liefert die gefilterten Fahrten der aktuellen Seite
  get pagedTrips(): Trip[] {
    return this.filteredTrips;
  }

  // Liefert die aktuell ausgewählte Fahrt, falls vorhanden
  get selectedTrip(): Trip | null {
    return this.selectedIndex !== null ? this.pagedTrips[this.selectedIndex] ?? null : null;
  }

  // Liefert die Diagrammdaten der ausgewählten Fahrt mit reduzierter Punktdichte
  get selectedTripChartData(): Trip[] {
    if (!this.selectedTrip) return [];

    return [{
      ...this.selectedTrip,
      points: this.sampleByInterval(this.selectedTrip.points, this.appliedSampleIntervalSeconds)
    }];
  }

  // Filtert die Fahrzeugliste anhand von Suchbegriff und erweiterten Filterkriterien
  get filteredVehicleOptions(): Vehicle[] {
    const term = this.vehicleSearch.toLowerCase();
    const f = this.appliedAdvancedVehicleFilter;

    return this.vehicles.filter(v => {
      // Einzelne Kriterien prüfen
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

  // Filtert die Telemetrie-Einheiten-IDs anhand des Suchbegriffs
  get filteredTelemetryOptions(): string[] {
    const term = this.telemetrySearch.toLowerCase();
    return this.telemetryUnits.map(u => u.id).filter(id => id.toLowerCase().includes(term));
  }

  // Filtert die Personenliste anhand von Suchbegriff und erweiterten Filterkriterien
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

  // Öffnet die Fahrzeug-Vorschlagsliste und setzt die Auswahl zurück, wenn das Suchfeld geleert wird
  onVehicleSearchChange(): void {
    this.showVehicleOptions = true;
    if (!this.vehicleSearch) this.filterVehicleId = '';
  }

  // Blendet den erweiterten Fahrzeugfilter ein oder aus
  toggleAdvancedVehicleFilter(): void {
    this.showAdvancedVehicleFilter = !this.showAdvancedVehicleFilter;
  }

  // Übernimmt die aktuell gewählten Filterwerte als angewendete Filter
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

  // Übernimmt ein aus der Vorschlagsliste gewähltes Fahrzeug in das Suchfeld
  selectVehicle(vehicle: Vehicle): void {
    this.filterVehicleId = vehicle.Id;
    this.vehicleSearch = vehicle.licensePlate ?? '';
    this.showVehicleOptions = false;
  }

  // Fügt das aktuell gewählte Fahrzeug der Filterliste hinzu (bis zum Maximum)
  addVehicleFilter(): void {
    if (this.filterVehicleId
      && !this.filterVehicleIds.includes(this.filterVehicleId)
      && this.filterVehicleIds.length < this.maxVehicleFilters) {
      this.filterVehicleIds.push(this.filterVehicleId);
    }

    this.filterVehicleId = '';
    this.vehicleSearch = '';
  }

  // Entfernt ein Fahrzeug aus der Filterliste
  removeVehicleFilter(vehicleId: string): void {
    this.filterVehicleIds = this.filterVehicleIds.filter(id => id !== vehicleId);
  }

  // Öffnet die Telemetrie-Einheiten-Vorschlagsliste und setzt die Auswahl zurück, wenn das Suchfeld geleert wird
  onTelemetrySearchChange(): void {
    this.showTelemetryOptions = true;
    if (!this.telemetrySearch) this.filterTelemetryUnitId = '';
  }

  // Übernimmt eine aus der Vorschlagsliste gewählte Telemetrie-Einheit in das Suchfeld
  selectTelemetryUnit(unitId: string): void {
    this.filterTelemetryUnitId = unitId;
    this.telemetrySearch = unitId;
    this.showTelemetryOptions = false;
  }

  // Fügt die aktuell gewählte Telemetrie-Einheit der Filterliste hinzu (bis zum Maximum)
  addTelemetryUnitFilter(): void {
    if (this.filterTelemetryUnitId
      && !this.filterTelemetryUnitIds.includes(this.filterTelemetryUnitId)
      && this.filterTelemetryUnitIds.length < this.maxTelemetryUnitFilters) {
      this.filterTelemetryUnitIds.push(this.filterTelemetryUnitId);
    }

    this.filterTelemetryUnitId = '';
    this.telemetrySearch = '';
  }

  // Entfernt eine Telemetrie-Einheit aus der Filterliste
  removeTelemetryUnitFilter(unitId: string): void {
    this.filterTelemetryUnitIds = this.filterTelemetryUnitIds.filter(id => id !== unitId);
  }

  // Öffnet die Personen-Vorschlagsliste und setzt die Auswahl zurück, wenn das Suchfeld geleert wird
  onPersonSearchChange(): void {
    this.showPersonOptions = true;
    if (!this.personSearch) this.filterPersonId = '';
  }

  // Blendet den erweiterten Personenfilter ein oder aus
  toggleAdvancedPersonFilter(): void {
    this.showAdvancedPersonFilter = !this.showAdvancedPersonFilter;
  }

  // Übernimmt eine aus der Vorschlagsliste gewählte Person in das Suchfeld
  selectPerson(person: Person): void {
    this.filterPersonId = person.Id;
    this.personSearch = person.Id;
    this.showPersonOptions = false;
  }

  // Fügt die aktuell gewählte Person der Filterliste hinzu (bis zum Maximum)
  addPersonFilter(): void {
    if (this.filterPersonId
      && !this.filterPersonIds.includes(this.filterPersonId)
      && this.filterPersonIds.length < this.maxPersonFilters) {
      this.filterPersonIds.push(this.filterPersonId);
    }

    this.filterPersonId = '';
    this.personSearch = '';
  }

  // Entfernt eine Person aus der Filterliste
  removePersonFilter(personId: string): void {
    this.filterPersonIds = this.filterPersonIds.filter(id => id !== personId);
  }

  // Wählt eine Fahrt in der Liste aus bzw. hebt die Auswahl bei erneutem Klick auf
  selectItem(index: number): void {
    this.selectedIndex = this.selectedIndex === index ? null : index;
  }

  // Verhindert das Schließen der Löschbestätigung, wenn in den Aktionsbereich geklickt wird
  onActionsClick(event: MouseEvent): void {
    event.stopPropagation();

    const target = event.target as HTMLElement;
    if (this.deleteConfirmIndex !== null && (!target.closest || !target.closest('.delete-btn'))) {
      this.deleteConfirmIndex = null;
    }
  }

  // Unterbindet den Standard-Fokuswechsel beim Klick auf einen Aktions-Button
  onActionMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  // Fordert bei erstem Klick eine Bestätigung an und löscht die Fahrt erst beim zweiten Klick
  confirmDelete(index: number, trip: Trip): void {
    if (!trip.end) return;

    if (this.deleteConfirmIndex !== index) {
      this.deleteConfirmIndex = index;
      return;
    }

    this.deleteTrip(trip);
  }

  // Löscht die Fahrt über den Service und aktualisiert die lokale Liste bzw. zeigt einen Fehler an
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

  // Erzeugt die Anzeigebezeichnung eines Fahrzeugs (ID plus Kennzeichen)
  vehicleLabel(vehicleId?: string | null): string {
    if (!vehicleId) return '';
    const vehicle = this.vehicles.find(v => v.Id === vehicleId);
    return vehicle ? `${vehicleId} - ${vehicle.licensePlate}` : vehicleId;
  }

  // Erzeugt die Anzeigebezeichnung eines Fahrers (ID plus Name)
  driverLabel(driverId?: string | null): string {
    if (!driverId) return '';
    const person = this.persons.find(p => p.Id === driverId);
    return person ? `${driverId} - ${person.firstName} ${person.lastName}` : driverId;
  }

  // Wechselt zur vorherigen Seite der Fahrtenliste, falls möglich
  prevTripPage(): void {
    if (this.currentTripPage > 1) {
      this.currentTripPage--;
      this.loadTripPage();
    }
  }

  // Wechselt zur nächsten Seite der Fahrtenliste, falls möglich
  nextTripPage(): void {
    if (this.currentTripPage < this.totalTripPages) {
      this.currentTripPage++;
      this.loadTripPage();
    }
  }

  // Springt direkt zu einer bestimmten Seite der Fahrtenliste
  goToTripPage(page: number): void {
    this.currentTripPage = page;
    this.loadTripPage();
  }

  // Wechselt zwischen Routen- und Detailansicht
  setViewMode(mode: 'routen' | 'details'): void {
    this.viewMode = mode;
  }

  // Zentriert die Karte über das Card-Widget neu
  recenterMap(): void {
    this.cardWidget?.recenter();
  }

  // Setzt alle Filter (einfach und erweitert) auf ihren Ausgangszustand zurück
  resetFilters(): void {
    this.filterVehicleId = '';
    this.filterVehicleIds = [];
    this.filterTelemetryUnitId = '';
    this.filterTelemetryUnitIds = [];
    this.filterPersonId = '';
    this.filterPersonIds = [];
    this.filterStart = '';
    this.filterStop = '';
    this.sampleIntervalSeconds = 10;
    this.appliedFilterVehicleIds = [];
    this.appliedFilterTelemetryUnitIds = [];
    this.appliedFilterPersonIds = [];
    this.appliedFilterStart = '';
    this.appliedFilterStop = '';
    this.appliedSampleIntervalSeconds = 10;
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

  // Erzeugt ein leeres Objekt für den erweiterten Personenfilter
  private emptyAdvancedPersonFilter() {
    return {
      firstName: '',
      lastName: '',
      birthDateFrom: '',
      birthDateTo: ''
    };
  }

  // Erzeugt ein leeres Objekt für den erweiterten Fahrzeugfilter
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

  // Dünnt die Punkte einer Fahrt aus, sodass nur alle intervalSeconds ein Punkt übernommen wird
  private sampleByInterval(points: TripPoint[], intervalSeconds: number): TripPoint[] {
    if (!points.length) return points;

    // Ersten Punkt immer übernehmen und Zeitfenster fortlaufend prüfen
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
}
