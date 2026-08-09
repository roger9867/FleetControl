import { Component, AfterViewInit, AfterViewChecked, OnChanges, SimpleChanges, Input } from '@angular/core';
import * as L from 'leaflet';

export interface VehicleMapPoint {
  id: string;
  label: string;
  lat: number;
  lng: number;
  telemetryUnitId?: string;
  speedKmh?: number;
  accelMs2?: number;
  timestamp?: string;
  driverLabel?: string;
  isMoving?: boolean;
}

@Component({
  selector: 'app-vehicle-map',
  imports: [],
  templateUrl: './vehicle-map.component.html',
  styleUrl: './vehicle-map.component.scss',
})
export class VehicleMap implements AfterViewInit, AfterViewChecked, OnChanges {

  @Input() points: VehicleMapPoint[] = [];

  private map!: L.Map;
  private mapInitialized = false;
  private markerLayer = L.layerGroup();
  private lastPointsSignature = '';

  ngAfterViewInit(): void {
    this.map = L.map('vehicle-map', {
      attributionControl: false
    }).setView([48.8375, 10.0936], 13);

    L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png', {
      attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a> &copy; OpenStreetMap contributors',
      maxZoom: 20,
      detectRetina: true,
      tileSize: 512,
      zoomOffset: -1
    }).addTo(this.map);

    this.markerLayer.addTo(this.map);
    this.mapInitialized = true;

    this.lastPointsSignature = this.pointsSignature();
    this.renderPoints();
  }

  ngAfterViewChecked(): void {
    if (this.mapInitialized) {
      this.map.invalidateSize();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['points'] || !this.mapInitialized) return;

    const signature = this.pointsSignature();
    if (signature === this.lastPointsSignature) return;

    this.lastPointsSignature = signature;
    this.renderPoints();
  }

  private pointsSignature(): string {
    return this.points.map(p => `${p.id}:${p.isMoving ? '1' : '0'}:${p.lat}:${p.lng}`).join('|');
  }

  private renderPoints(): void {
    this.markerLayer.clearLayers();

    this.points.forEach(point => {
      const coord: L.LatLngExpression = [point.lat, point.lng];

      const pulse = point.isMoving ? '<span class="vehicle-marker-pulse"></span>' : '';

      const icon = L.divIcon({
        className: 'vehicle-marker-icon',
        html: `<div class="vehicle-marker">${pulse}<span class="vehicle-marker-dot"></span></div>`,
        iconSize: [24, 24],
        iconAnchor: [12, 12]
      });

      const marker = L.marker(coord, { icon }).addTo(this.markerLayer);

      marker.bindTooltip(this.tooltipContent(point), { permanent: false, direction: 'top' });
    });

    this.recenter();
  }

  private tooltipContent(point: VehicleMapPoint): string {
    const lines = [`Fahrzeug: ${point.label}`];

    if (point.timestamp) {
      lines.push(`Zeitstempel: ${new Date(point.timestamp).toLocaleString('de-DE')}`);
    }
    if (point.telemetryUnitId) {
      lines.push(`T-Einheit: ${point.telemetryUnitId}`);
    }
    if (point.driverLabel) {
      lines.push(`Fahrer: ${point.driverLabel}`);
    }
    if (point.speedKmh != null) {
      lines.push(`Geschw.: ${point.speedKmh.toFixed(1)} km/h`);
    }
    if (point.accelMs2 != null) {
      lines.push(`Beschleunigung: ${point.accelMs2.toFixed(2)} m/s²`);
    }

    return lines.join('<br>');
  }

  recenter(): void {
    const coordinates: L.LatLngExpression[] = this.points.map(p => [p.lat, p.lng]);

    if (coordinates.length) {
      this.map.fitBounds(L.latLngBounds(coordinates), { padding: [40, 40], maxZoom: 16 });
    }
  }
}
