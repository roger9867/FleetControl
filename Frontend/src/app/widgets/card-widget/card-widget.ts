import { Component, AfterViewInit, AfterViewChecked, ViewChild, ElementRef } from '@angular/core';
import * as L from 'leaflet';

@Component({
  selector: 'app-card-widget',
  imports: [],
  templateUrl: './card-widget.html',
  styleUrl: './card-widget.scss',
})
export class CardWidget implements AfterViewInit, AfterViewChecked {

  private map!: L.Map;
  private mapInitialized = false;

  ngAfterViewInit(): void {
    this.map = L.map('map', {
      attributionControl: false
    }).setView([50.9271, 11.5892], 17);
    
    
L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png', {
    attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a> &copy; OpenStreetMap contributors',
    maxZoom: 20,
    detectRetina: true,
    tileSize: 512,
    zoomOffset: -1
}).addTo(this.map);
    this.mapInitialized = true;

    this.addRouteWithClickablePoints();
  }

  ngAfterViewChecked(): void {
    if (this.mapInitialized) {
      this.map.invalidateSize();
    }
  }

private addRouteWithClickablePoints() {
  const routeCoordinates: L.LatLngExpression[] = [
    [50.9271, 11.5892],
    [50.9275, 11.5896],
    [50.9280, 11.5900],
    [50.9290, 11.5910]
  ];

  const polyline = L.polyline(routeCoordinates, { 
    color: 'red', 
    weight: 4, 
    lineJoin: 'round',
    fillOpacity: 0.9,
  }).addTo(this.map);

  routeCoordinates.forEach((coord, index) => {
    const marker = L.circleMarker(coord, {
      radius: 6,
      color: 'red',
      fillColor: 'red',
      fillOpacity: 0.9,
      weight: 2
    }).addTo(this.map);

    marker.bindTooltip(`Punkt ${index + 1}`, { permanent: false, direction: 'top' });

    marker.on('click', () => {
      console.log(`Du hast Punkt ${index + 1} geklickt!`);
      alert(`Punkt ${index + 1} ausgewählt`);
    });
  });
}
}
