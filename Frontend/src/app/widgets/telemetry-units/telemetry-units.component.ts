import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectorRef } from '@angular/core';

import { TelemetryUnitService } from '../../services/telemetry-unit.service';
import { TelemetryUnit } from '../../models/telemetry-unit.model';

@Component({
  selector: 'app-telemetry-units',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './telemetry-units.component.html',
  styleUrls: ['./telemetry-units.component.scss']
})
export class TelemetryUnits implements OnInit
{
  usb_connected_units: TelemetryUnit[] = [];

  registered_units: TelemetryUnit[] = [];

  selectedIndex: number | null = null;
  selectedUnitId: string | null = null;

  constructor(
    private service: TelemetryUnitService,
    private cdr: ChangeDetectorRef
  ) {

  }

  ngOnInit(): void {
    this.loadAllUnits();
    this.sendBroadcast();
  }

  createUnit(): void {
    console.log('BUTTON CLICKED');

    if (!this.selectedUnitId) return;
    console.log('NO UNIT SELECTED');
    const dto = {
      id: this.selectedUnitId
    };

    this.service.createUnit(dto)
      .subscribe({
        next: () => {
          console.log('CREATED');
          this.loadAllUnits(); // refresh DB list
          this.selectedUnitId = null;
        },
        error: (err) => {
          console.error('CREATE FAILED', err);
        }
      });
  }

  selectItem(index: number): void {
    this.selectedIndex =
      this.selectedIndex === index ? null : index;
  }

  loadAllUnits(): void {
    this.service.getUnits()
      .subscribe({
        next: (res) => {
          this.registered_units = res ?? [];
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('GET FAILED', err);
          this.registered_units = [];
        }
      });
  }

  sendBroadcast(): void {

  console.log('SEND START');

  this.service.broadcastCommand()
    .subscribe(res => {

      console.log('RAW RESPONSE:', res);

      const values = Object.values(res ?? {}).filter(Boolean);

      console.log('EXTRACTED:', values);

      this.usb_connected_units = (values as string[]).map(id => ({
        id
      }));

      console.log('FINAL LIVE UNITS:', this.usb_connected_units);

      this.cdr.detectChanges();
    });
  }
}
