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
export class CardWidgetComponent implements AfterViewInit, AfterViewChecked, OnChanges {

  @Input() trips: Trip[] = [];
  @Input() vehicles: Vehicle[] = [];

  private map!: L.Map;
  private mapInitialized = false;
  private routeLayer = L.layerGroup();
  private lastTripsSignature = '';

  private readonly routeColors = ['#e74c3c', '#3498db', '#2ecc71', '#f1c40f', '#9b59b6'];

  // Creates the Leaflet map including the tile layer and renders the first trip routes.
  ngAfterViewInit(): void {
    // 1. Initialize the map and set the initial view
    this.map = L.map('map', {
      attributionControl: false
    }).setView([48.8375, 10.0936], 14);


// 2. Load map tiles
L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png', {
    attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a> &copy; OpenStreetMap contributors',
    maxZoom: 20,
    detectRetina: true,
    tileSize: 512,
    zoomOffset: -1
}).addTo(this.map);

    // 3. Attach the route layer and draw the first trips
    this.routeLayer.addTo(this.map);
    this.mapInitialized = true;

    this.lastTripsSignature = this.tripsSignature();
    this.renderTrips();
  }

  // Adjusts the map size to the current layout after each change detection
  ngAfterViewChecked(): void {
    if (this.mapInitialized) {
      this.map.invalidateSize();
    }
  }

  // Re-renders the routes as soon as the given trips have actually changed
  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['trips'] || !this.mapInitialized) return;

    const signature = this.tripsSignature();
    if (signature === this.lastTripsSignature) return;

    this.lastTripsSignature = signature;
    this.renderTrips();
  }

  // Creates a comparable signature of all trips to avoid unnecessary re-rendering
  private tripsSignature(): string {
    return this.trips.map(t => `${t.id}:${t.points.length}:${t.end ? '1' : '0'}`).join('|');
  }

  // Draws the route with points and start/end markers for each trip and then centers the map
  private renderTrips(): void {
    this.routeLayer.clearLayers();

    if (!this.trips.length) {
      return;
    }

    const allPoints: L.LatLngExpression[] = [];

    this.trips.forEach((trip, tripIndex) => {
      const color = this.routeColors[tripIndex % this.routeColors.length];
      const coordinates: L.LatLngExpression[] = trip.points.map(p => [p.lat, p.lng]);
      const isOngoing = !trip.end;

      // Draw the route as a line
      L.polyline(coordinates, {
        color,
        weight: 4,
        lineJoin: 'round',
        opacity: 0.65,
      }).addTo(this.routeLayer);

      // Draw a point marker for each trip point (highlight the current position of a running trip with a pulse)
      trip.points.forEach((point, index) => {
        const isLastPoint = index === trip.points.length - 1;

        if (isOngoing && isLastPoint) {
          const icon = L.divIcon({
            className: 'trip-marker-icon',
            html: `<div class="trip-marker">`
              + `<span class="trip-marker-pulse" style="background-color:${color}"></span>`
              + `<span class="trip-marker-dot" style="background-color:${color};border-color:${color}"></span>`
              + `</div>`,
            iconSize: [16, 16],
            iconAnchor: [8, 8]
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

      // Draw start and (if finished) end markers
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

  // Centers and zooms the map so that all trip points are visible
  recenter(): void {
    const allPoints: L.LatLngExpression[] = this.trips.flatMap(trip =>
      trip.points.map(p => [p.lat, p.lng] as L.LatLngExpression)
    );

    if (allPoints.length) {
      this.map.fitBounds(L.latLngBounds(allPoints), { padding: [30, 30] });
    }
  }

  // Builds the tooltip HTML for a trip point from vehicle, driver and measured values
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

  // Determines the license plate of the vehicle for a vehicle ID.
  private licensePlate(vehicleId?: string | null): string {
    if (!vehicleId) return '–';
    const vehicle = this.vehicles.find(v => v.Id === vehicleId);
    return vehicle?.licensePlate || '–';
  }
}
