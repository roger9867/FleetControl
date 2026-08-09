import { Component, AfterViewInit, AfterViewChecked, OnChanges, SimpleChanges, Input } from '@angular/core';
import * as L from 'leaflet';

import { Trip, TripPoint } from '../../models/trip.model';
import { Vehicle } from '../../models/vehicle.model';

@Component({
  selector: 'app-card-widget',
  imports: [],
  templateUrl: './card-widget.component.html',
  styleUrl: './card-widget.component.scss',
})
export class CardWidget implements AfterViewInit, AfterViewChecked, OnChanges {

  @Input() trips: Trip[] = [];
  @Input() vehicles: Vehicle[] = [];

  private map!: L.Map;
  private mapInitialized = false;
  private routeLayer = L.layerGroup();
  private lastTripsSignature = '';

  private readonly routeColors = ['#e74c3c', '#3498db', '#2ecc71', '#f1c40f', '#9b59b6'];

  ngAfterViewInit(): void {
    this.map = L.map('map', {
      attributionControl: false
    }).setView([48.8375, 10.0936], 14);


L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png', {
    attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a> &copy; OpenStreetMap contributors',
    maxZoom: 20,
    detectRetina: true,
    tileSize: 512,
    zoomOffset: -1
}).addTo(this.map);

    this.routeLayer.addTo(this.map);
    this.mapInitialized = true;

    this.lastTripsSignature = this.tripsSignature();
    this.renderTrips();
  }

  ngAfterViewChecked(): void {
    if (this.mapInitialized) {
      this.map.invalidateSize();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['trips'] || !this.mapInitialized) return;

    const signature = this.tripsSignature();
    if (signature === this.lastTripsSignature) return;

    this.lastTripsSignature = signature;
    this.renderTrips();
  }

  private tripsSignature(): string {
    return this.trips.map(t => `${t.id}:${t.points.length}:${t.end ? '1' : '0'}`).join('|');
  }

  private renderTrips(): void {
    this.routeLayer.clearLayers();

    if (!this.trips.length) {
      // this.addDummyRoute();
      return;
    }

    const allPoints: L.LatLngExpression[] = [];

    this.trips.forEach((trip, tripIndex) => {
      const color = this.routeColors[tripIndex % this.routeColors.length];
      const coordinates: L.LatLngExpression[] = trip.points.map(p => [p.lat, p.lng]);
      const isOngoing = !trip.end;

      L.polyline(coordinates, {
        color,
        weight: 4,
        lineJoin: 'round',
        opacity: 0.65,
      }).addTo(this.routeLayer);

      trip.points.forEach((point, index) => {
        const isLastPoint = index === trip.points.length - 1;

        if (isOngoing && isLastPoint) {
          // Laufende Fahrt: der aktuellste Punkt pulsiert wie auf der
          // Fahrzeuge-Seite, statt als normaler kleiner Punkt zu erscheinen -
          // hier aber in der Farbe der jeweiligen Strecke statt der festen
          // Akzentfarbe, per Inline-Style ueber die geteilte CSS-Klasse gelegt.
          const icon = L.divIcon({
            className: 'vehicle-marker-icon',
            html: `<div class="vehicle-marker">`
              + `<span class="vehicle-marker-pulse" style="background-color:${color}"></span>`
              + `<span class="vehicle-marker-dot" style="background-color:${color};border-color:${color}"></span>`
              + `</div>`,
            iconSize: [24, 24],
            iconAnchor: [12, 12]
          });

          const marker = L.marker(coordinates[index], { icon }).addTo(this.routeLayer);
          marker.bindTooltip(this.pointTooltip(trip, point), { permanent: false, direction: 'top' });
          return;
        }

        const marker = L.circleMarker(coordinates[index], {
          radius: 5,
          color,
          fillColor: color,
          fillOpacity: 1,
          opacity: 1,
          weight: 2
        }).addTo(this.routeLayer);

        marker.bindTooltip(this.pointTooltip(trip, point), { permanent: false, direction: 'top' });
      });

      if (coordinates.length) {
        const startMarker = L.circleMarker(coordinates[0], {
          radius: 12,
          color,
          fillColor: color,
          fillOpacity: 1,
          opacity: 1,
          weight: 2
        }).addTo(this.routeLayer);
        startMarker.bindTooltip(
          this.pointTooltip(trip, trip.points[0], 'Start'),
          { permanent: false, direction: 'top' }
        );

        if (!isOngoing) {
          const endCoord = coordinates[coordinates.length - 1];
          const endMarker = L.circleMarker(endCoord, {
            radius: 14,
            color,
            fillOpacity: 0,
            opacity: 1,
            weight: 3
          }).addTo(this.routeLayer);
          endMarker.bindTooltip(
            this.pointTooltip(trip, trip.points[trip.points.length - 1], 'Ziel'),
            { permanent: false, direction: 'top' }
          );
        }
      }

      allPoints.push(...coordinates);
    });

    this.recenter();
  }

  recenter(): void {
    const allPoints: L.LatLngExpression[] = this.trips.flatMap(trip =>
      trip.points.map(p => [p.lat, p.lng] as L.LatLngExpression)
    );

    if (allPoints.length) {
      this.map.fitBounds(L.latLngBounds(allPoints), { padding: [30, 30] });
    }
  }

  private pointTooltip(trip: Trip, point: TripPoint, label?: string): string {
    const time = new Date(point.timestamp).toLocaleString('de-DE');
    const zeitLine = label ? `Zeit: ${time} — ${label}` : `Zeit: ${time}`;

    return `Kennzeichen: ${this.licensePlate(trip.vehicleId)}<br>`
      + `T-Einheit-ID: ${trip.telemetryUnitId}<br>`
      + `Fahrer: ${trip.driverId ?? '–'}<br>`
      + `${zeitLine}<br>`
      + `Geschwindigkeit: ${point.speedKmh.toFixed(1)} km/h<br>`
      + `Beschleunigung: ${point.accelMs2.toFixed(2)} m/s²<br>`
      + `Koordinaten: ${point.lat.toFixed(6)}, ${point.lng.toFixed(6)}`;
  }

  private licensePlate(vehicleId?: string | null): string {
    if (!vehicleId) return '–';
    const vehicle = this.vehicles.find(v => v.Id === vehicleId);
    return vehicle?.licensePlate || '–';
  }

  // private addDummyRoute(): void {
  //   const routeCoordinates: L.LatLngExpression[] = [
  //     [50.9271, 11.5892],
  //     [50.9275, 11.5896],
  //     [50.9280, 11.5900],
  //     [50.9290, 11.5910]
  //   ];
  //
  //   L.polyline(routeCoordinates, {
  //     color: 'red',
  //     weight: 4,
  //     lineJoin: 'round',
  //     opacity: 0.65,
  //   }).addTo(this.routeLayer);
  //
  //   routeCoordinates.forEach((coord, index) => {
  //     const marker = L.circleMarker(coord, {
  //       radius: 6,
  //       color: 'red',
  //       fillColor: 'red',
  //       fillOpacity: 1,
  //       opacity: 1,
  //       weight: 2
  //     }).addTo(this.routeLayer);
  //
  //     marker.bindTooltip(`Punkt ${index + 1}`, { permanent: false, direction: 'top' });
  //   });
  //
  //   L.circleMarker(routeCoordinates[0], {
  //     radius: 13,
  //     color: 'red',
  //     fillOpacity: 0,
  //     opacity: 1,
  //     weight: 3
  //   }).addTo(this.routeLayer).bindTooltip('Start', { permanent: false, direction: 'top' });
  //
  //   L.circleMarker(routeCoordinates[routeCoordinates.length - 1], {
  //     radius: 13,
  //     color: 'red',
  //     fillColor: 'red',
  //     fillOpacity: 1,
  //     opacity: 1,
  //     weight: 2
  //   }).addTo(this.routeLayer).bindTooltip('Ziel', { permanent: false, direction: 'top' });
  // }
}
