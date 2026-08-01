import { Component, AfterViewInit, AfterViewChecked, OnChanges, SimpleChanges, Input } from '@angular/core';
import * as L from 'leaflet';

import { Trip } from '../../models/trip.model';

@Component({
  selector: 'app-card-widget',
  imports: [],
  templateUrl: './card-widget.component.html',
  styleUrl: './card-widget.component.scss',
})
export class CardWidget implements AfterViewInit, AfterViewChecked, OnChanges {

  @Input() trips: Trip[] = [];

  private map!: L.Map;
  private mapInitialized = false;
  private routeLayer = L.layerGroup();

  private readonly routeColors = ['#e74c3c', '#3498db', '#2ecc71', '#f1c40f', '#9b59b6'];

  ngAfterViewInit(): void {
    this.map = L.map('map', {
      attributionControl: false
    }).setView([50.9271, 11.5892], 14);


L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png', {
    attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a> &copy; OpenStreetMap contributors',
    maxZoom: 20,
    detectRetina: true,
    tileSize: 512,
    zoomOffset: -1
}).addTo(this.map);

    this.routeLayer.addTo(this.map);
    this.mapInitialized = true;

    this.renderTrips();
  }

  ngAfterViewChecked(): void {
    if (this.mapInitialized) {
      this.map.invalidateSize();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['trips'] && this.mapInitialized) {
      this.renderTrips();
    }
  }

  private renderTrips(): void {
    this.routeLayer.clearLayers();

    if (!this.trips.length) {
      this.addDummyRoute();
      return;
    }

    const allPoints: L.LatLngExpression[] = [];

    this.trips.forEach((trip, tripIndex) => {
      const color = this.routeColors[tripIndex % this.routeColors.length];
      const coordinates: L.LatLngExpression[] = trip.points.map(p => [p.lat, p.lng]);

      L.polyline(coordinates, {
        color,
        weight: 4,
        lineJoin: 'round',
        fillOpacity: 0.9,
      }).addTo(this.routeLayer);

      coordinates.forEach((coord, index) => {
        const marker = L.circleMarker(coord, {
          radius: 5,
          color,
          fillColor: color,
          fillOpacity: 0.9,
          weight: 2
        }).addTo(this.routeLayer);

        marker.bindTooltip(`${trip.id} - Punkt ${index + 1}`, { permanent: false, direction: 'top' });
      });

      allPoints.push(...coordinates);
    });

    if (allPoints.length) {
      this.map.fitBounds(L.latLngBounds(allPoints), { padding: [30, 30] });
    }
  }

  private addDummyRoute(): void {
    const routeCoordinates: L.LatLngExpression[] = [
      [50.9271, 11.5892],
      [50.9275, 11.5896],
      [50.9280, 11.5900],
      [50.9290, 11.5910]
    ];

    L.polyline(routeCoordinates, {
      color: 'red',
      weight: 4,
      lineJoin: 'round',
      fillOpacity: 0.9,
    }).addTo(this.routeLayer);

    routeCoordinates.forEach((coord, index) => {
      const marker = L.circleMarker(coord, {
        radius: 6,
        color: 'red',
        fillColor: 'red',
        fillOpacity: 0.9,
        weight: 2
      }).addTo(this.routeLayer);

      marker.bindTooltip(`Punkt ${index + 1}`, { permanent: false, direction: 'top' });
    });
  }
}
