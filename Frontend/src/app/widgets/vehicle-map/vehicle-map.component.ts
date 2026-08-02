import { Component, AfterViewInit, AfterViewChecked, OnChanges, SimpleChanges, Input } from '@angular/core';
import * as L from 'leaflet';

export interface VehicleMapPoint {
  id: string;
  label: string;
  lat: number;
  lng: number;
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

  ngAfterViewInit(): void {
    this.map = L.map('vehicle-map', {
      attributionControl: false
    }).setView([50.9271, 11.5892], 13);

    L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png', {
      attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a> &copy; OpenStreetMap contributors',
      maxZoom: 20,
      detectRetina: true,
      tileSize: 512,
      zoomOffset: -1
    }).addTo(this.map);

    this.markerLayer.addTo(this.map);
    this.mapInitialized = true;

    this.renderPoints();
  }

  ngAfterViewChecked(): void {
    if (this.mapInitialized) {
      this.map.invalidateSize();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['points'] && this.mapInitialized) {
      this.renderPoints();
    }
  }

  private renderPoints(): void {
    this.markerLayer.clearLayers();

    const coordinates: L.LatLngExpression[] = [];

    this.points.forEach(point => {
      const coord: L.LatLngExpression = [point.lat, point.lng];
      coordinates.push(coord);

      const marker = L.circleMarker(coord, {
        radius: 7,
        color: '#373669',
        fillColor: '#7376e0',
        fillOpacity: 0.9,
        weight: 2
      }).addTo(this.markerLayer);

      marker.bindTooltip(point.label, { permanent: false, direction: 'top' });
    });

    if (coordinates.length) {
      this.map.fitBounds(L.latLngBounds(coordinates), { padding: [40, 40] });
    }
  }
}
