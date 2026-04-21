import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

import { CardWidget } from '../card-widget/card-widget';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, CardWidget],
  templateUrl: './fahrten.component.html',
  styleUrls: ['./fahrten.component.scss']
})
export class LayoutComponent {
  items = Array.from({ length: 20 }, (_, i) => `Item ${i + 1}`);

  selectedIndex: number | null = null;

  selectItem(index: number) {
    this.selectedIndex = this.selectedIndex === index ? null : index;
  }
}
