import { Injectable, NgZone } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

const BAUD_RATE = 115200;
const IDENTIFY_COMMAND = 'get_device_id';
const RESPONSE_TIMEOUT_MS = 4000;
const UUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

interface PortReaderState {
  lineListeners: Array<(line: string) => boolean>;
}

@Injectable({ providedIn: 'root' })
export class UsbTelemetryService {

  private connectedUnitIdSubject = new Subject<string>();
  connectedUnitId$ = this.connectedUnitIdSubject.asObservable();

  private connectedUnitIdsSubject = new BehaviorSubject<string[]>([]);
  connectedUnitIds$ = this.connectedUnitIdsSubject.asObservable();

  private deviceIds = new Map<SerialPort, string>();

  // One persistent background reader per port instead of a new reader per request
  private portReaders = new Map<SerialPort, PortReaderState>();

  // Checks whether the browser supports the Web Serial API.
  get isSupported(): boolean {
    return 'serial' in navigator;
  }

  // Registers connect/disconnect listeners and identifies already connected devices at startup.
  constructor(private zone: NgZone) {
    if (!this.isSupported) return;

    // 1. Automatically identify newly connected devices
    navigator.serial.addEventListener('connect', event => {
      this.zone.run(() => {
        this.identifyPort(event.target as SerialPort).catch(() => {});
      });
    });

    // 2. On disconnect, clean up reader and device state
    navigator.serial.addEventListener('disconnect', event => {
      this.zone.run(() => {
        this.portReaders.delete(event.target as SerialPort);
        this.deviceIds.delete(event.target as SerialPort);
        this.emitConnectedUnitIds();
      });
    });

    // 3. Directly identify ports already authorized by the browser
    navigator.serial.getPorts().then(ports => {
      ports.forEach(port => this.identifyPort(port).catch(() => {}));
    });
  }

  // Opens the browser dialog for device selection and identifies the selected port.
  async requestDevice(): Promise<void> {
    if (!this.isSupported) {
      throw new Error('Web Serial wird von diesem Browser nicht unterstuetzt (nur Chrome/Edge/Opera).');
    }

    const port = await navigator.serial.requestPort();
    await this.identifyPort(port);
  }

  // Identifies all already authorized ports, or opens the selection dialog if none is known.
  async broadcastToAll(): Promise<void> {
    if (!this.isSupported) {
      throw new Error('Web Serial wird von diesem Browser nicht unterstuetzt (nur Chrome/Edge/Opera).');
    }

    const ports = await navigator.serial.getPorts();

    if (ports.length === 0) {
      await this.requestDevice();
      return;
    }

    await Promise.all(ports.map(port => this.identifyPort(port).catch(() => {})));
  }

  // Queries the device ID of a port and updates the internal state on success.
  private async identifyPort(port: SerialPort): Promise<void> {
    const id = await this.readDeviceId(port);
    if (id) {
      this.deviceIds.set(port, id);
      this.emitConnectedUnitIds();
      this.connectedUnitIdSubject.next(id);
    }
  }

  // Sends the current list of unique connected device IDs to all subscribers.
  private emitConnectedUnitIds(): void {
    this.connectedUnitIdsSubject.next(Array.from(new Set(this.deviceIds.values())));
  }

  // Sends the identify command to the port and waits for the returned device UUID.
  private async readDeviceId(port: SerialPort): Promise<string | null> {
    // 1. Ensure the background reader and start waiting for the response
    const state = await this.ensureReaderStarted(port);
    const responsePromise = this.waitForUuid(state, RESPONSE_TIMEOUT_MS);

    // 2. Write the identify command to the device
    const writer = port.writable!.getWriter();
    try {
      console.log('[USB %s] write get_device_id', performance.now().toFixed(0));
      await writer.write(new TextEncoder().encode(`${IDENTIFY_COMMAND}\n`));
    } finally {
      writer.releaseLock();
    }

    // 3. Wait for the response and return the result
    const result = await responsePromise;
    console.log('[USB %s] readDeviceId resolved with', performance.now().toFixed(0), result);
    return result;
  }

  // Returns the existing reader state of a port, or opens the port and starts a new background reader.
  private async ensureReaderStarted(port: SerialPort): Promise<PortReaderState> {
    const existing = this.portReaders.get(port);
    if (existing) {
      return existing;
    }

    if (port.readable === null) {
      await port.open({ baudRate: BAUD_RATE });
    }

    const state: PortReaderState = { lineListeners: [] };
    this.portReaders.set(port, state);

    // Intentionally not awaited: keeps running in the background for the entire
    // lifetime of the port.
    this.pumpLines(port, state);

    return state;
  }

  // Continuously reads bytes from the port, assembles them into lines and notifies matching listeners.
  private async pumpLines(port: SerialPort, state: PortReaderState): Promise<void> {
    const reader = port.readable!.getReader();
    let bytes: number[] = [];

    try {
      while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        if (!value) continue;

        for (const byte of value) {
          if (byte === 0x0A) {
            // End of line detected: decode bytes to text
            const text = new TextDecoder().decode(new Uint8Array(bytes)).trim();
            bytes = [];

            if (text) {
              console.log('[USB %s] line:', performance.now().toFixed(0), JSON.stringify(text));

              // The first matching listener consumes the line, all
              // others remain for the next line.
              const idx = state.lineListeners.findIndex(fn => fn(text));
              if (idx !== -1) {
                state.lineListeners.splice(idx, 1);
              }
            }
            continue;
          }
          bytes.push(byte);
        }
      }
    } catch {
      // Port closed/disconnected -> simply end the background loop.
    } finally {
      reader.releaseLock();
      this.portReaders.delete(port);
    }
  }

  // Waits for a line matching the UUID pattern, or aborts after the timeout expires.
  private waitForUuid(state: PortReaderState, timeoutMs: number): Promise<string | null> {
    return new Promise(resolve => {
      const listener = (line: string): boolean => {
        if (!UUID_PATTERN.test(line)) {
          return false;
        }
        clearTimeout(timer);
        resolve(line);
        return true;
      };

      const timer = setTimeout(() => {
        const idx = state.lineListeners.indexOf(listener);
        if (idx !== -1) {
          state.lineListeners.splice(idx, 1);
        }
        resolve(null);
      }, timeoutMs);

      state.lineListeners.push(listener);
    });
  }
}
