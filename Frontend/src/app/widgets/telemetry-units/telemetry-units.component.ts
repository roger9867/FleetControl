import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

//import { TelemetryUnits } from '../telemetry-units/telemetry-units';

@Component({
  selector: 'app-telemetry-units',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './telemetry-units.component.html',
  styleUrls: ['./telemetry-units.component.scss']
})
export class TelemetryUnits {

  items: string[] = [];

  ngOnInit() {
    this.loadAllTelemetryUnits();
  }

  selectedIndex: number | null = null;

  selectItem(index: number) {
    this.selectedIndex = this.selectedIndex === index ? null : index;
  }

  loadAllTelemetryUnits() {
    this.service.getItems()
      .subscribe(res => {
        this.items = res ?? [];
      });
  }
}
   