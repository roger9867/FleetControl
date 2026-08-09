import { Injectable, NgZone } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

const USB_INTERFACE_NUMBER = 0;
const USB_ENDPOINT_OUT = 1;
const USB_ENDPOINT_IN = 1;
const IDENTIFY_COMMAND = 'get_device_id';
const RESPONSE_TIMEOUT_MS = 4000;

@Injectable({ providedIn: 'root' })
export class UsbTelemetryService {

  private connectedUnitIdSubject = new Subject<string>();
  connectedUnitId$ = this.connectedUnitIdSubject.asObservable();

  private connectedUnitIdsSubject = new BehaviorSubject<string[]>([]);
  connectedUnitIds$ = this.connectedUnitIdsSubject.asObservable();

  private deviceIds = new Map<USBDevice, string>();

  get isSupported(): boolean {
    return 'usb' in navigator;
  }

  constructor(private zone: NgZone) {
    if (!this.isSupported) return;

    navigator.usb.addEventListener('connect', event => {
      this.zone.run(() => {
        this.identifyDevice(event.device).catch(() => {});
      });
    });

    navigator.usb.addEventListener('disconnect', event => {
      this.zone.run(() => {
        this.deviceIds.delete(event.device);
        this.emitConnectedUnitIds();
      });
    });

    navigator.usb.getDevices().then(devices => {
      devices.forEach(device => this.identifyDevice(device).catch(() => {}));
    });
  }

  async requestDevice(): Promise<void> {
    if (!this.isSupported) {
      throw new Error('WebUSB wird von diesem Browser nicht unterstuetzt (nur Chrome/Edge/Opera).');
    }

    const device = await navigator.usb.requestDevice({ filters: [{}] });
    await this.identifyDevice(device);
  }

  private async identifyDevice(device: USBDevice): Promise<void> {
    const id = await this.readDeviceId(device);
    if (id) {
      this.deviceIds.set(device, id);
      this.emitConnectedUnitIds();
      this.connectedUnitIdSubject.next(id);
    }
  }

  private emitConnectedUnitIds(): void {
    this.connectedUnitIdsSubject.next(Array.from(new Set(this.deviceIds.values())));
  }

  private async readDeviceId(device: USBDevice): Promise<string | null> {
    await device.open();

    if (device.configuration === null) {
      await device.selectConfiguration(1);
    }

    await device.claimInterface(USB_INTERFACE_NUMBER);

    const command = new TextEncoder().encode(`${IDENTIFY_COMMAND}\n`);
    await device.transferOut(USB_ENDPOINT_OUT, command);

    return this.readLine(device);
  }

  private async readLine(device: USBDevice): Promise<string | null> {
    const bytes: number[] = [];
    const deadline = Date.now() + RESPONSE_TIMEOUT_MS;

    while (Date.now() < deadline) {
      const result = await device.transferIn(USB_ENDPOINT_IN, 64);
      if (!result.data) continue;

      for (let i = 0; i < result.data.byteLength; i++) {
        const byte = result.data.getUint8(i);

        if (byte === 0x0A) {
          const text = new TextDecoder().decode(new Uint8Array(bytes)).trim();
          return text || null;
        }

        bytes.push(byte);
      }
    }

    return null;
  }
}
