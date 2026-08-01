import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';

import { Trip } from '../../models/trip.model';

@Component({
  selector: 'app-trip-chart',
  standalone: true,
  imports: [NgxEchartsDirective],
  templateUrl: './trip-chart.component.html',
  styleUrls: ['./trip-chart.component.scss']
})
export class TripChart implements OnChanges {

  @Input() trips: Trip[] = [];

  chartOptions: EChartsOption = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['trips']) {
      this.chartOptions = this.buildChartOptions();
    }
  }

  private buildChartOptions(): EChartsOption {
    const points = this.trips.flatMap(trip => trip.points);

    const labels = points.map(p =>
      new Date(p.timestamp).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
    );
    const speed = points.map(p => Math.round(p.speedKmh * 10) / 10);
    const accel = points.map(p => Math.round(p.accelMs2 * 100) / 100);

    return {
      backgroundColor: 'transparent',
      textStyle: { color: '#ccc' },
      tooltip: { trigger: 'axis' },
      legend: {
        data: ['Geschwindigkeit (km/h)', 'Beschleunigung (m/s²)'],
        textStyle: { color: '#ccc' },
        top: 0
      },
      grid: { left: 55, right: 55, top: 50, bottom: 80 },
      xAxis: {
        type: 'category',
        data: labels,
        axisLine: { lineStyle: { color: '#555' } },
        axisLabel: { color: '#999' },
        splitLine: { show: true, lineStyle: { color: '#4a4a55', type: 'dashed' } }
      },
      yAxis: [
        {
          type: 'value',
          name: 'km/h',
          position: 'left',
          axisLine: { lineStyle: { color: '#555' } },
          axisLabel: { color: '#999' },
          splitLine: { show: true, lineStyle: { color: '#4a4a55', type: 'dashed' } }
        },
        {
          type: 'value',
          name: 'm/s²',
          position: 'right',
          axisLine: { lineStyle: { color: '#555' } },
          axisLabel: { color: '#999' },
          splitLine: { show: false }
        }
      ],
      dataZoom: [
        { type: 'inside', xAxisIndex: 0 },
        {
          type: 'slider',
          xAxisIndex: 0,
          bottom: 10,
          height: 22,
          textStyle: { color: '#999' },
          borderColor: '#2a2b33',
          fillerColor: 'rgba(115, 118, 224, 0.2)',
          handleStyle: { color: '#373669' }
        }
      ],
      series: [
        {
          name: 'Geschwindigkeit (km/h)',
          type: 'line',
          smooth: true,
          showSymbol: false,
          data: speed,
          yAxisIndex: 0,
          color: '#e74c3c'
        },
        {
          name: 'Beschleunigung (m/s²)',
          type: 'line',
          smooth: true,
          showSymbol: false,
          data: accel,
          yAxisIndex: 1,
          color: '#3498db'
        }
      ]
    };
  }
}
