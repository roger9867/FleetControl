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

  // Ein dauerhafter Hintergrund-Reader pro Port statt eines neuen Readers
  // pro Anfrage: die Debug-UART sendet laufend Log-Zeilen, die sonst als
  // Rückstau vor der echten Antwort abgearbeitet werden müssten. So wird
  // von Anfang an kontinuierlich mitgelesen und verworfen, es kann sich
  // also gar kein Rückstau aufbauen.
  private portReaders = new Map<SerialPort, PortReaderState>();

  get isSupported(): boolean {
    return 'serial' in navigator;
  }

  constructor(private zone: NgZone) {
    if (!this.isSupported) return;

    navigator.serial.addEventListener('connect', event => {
      this.zone.run(() => {
        this.identifyPort(event.target as SerialPort).catch(() => {});
      });
    });

    navigator.serial.addEventListener('disconnect', event => {
      this.zone.run(() => {
        this.portReaders.delete(event.target as SerialPort);
        this.deviceIds.delete(event.target as SerialPort);
        this.emitConnectedUnitIds();
      });
    });

    navigator.serial.getPorts().then(ports => {
      ports.forEach(port => this.identifyPort(port).catch(() => {}));
    });
  }

  async requestDevice(): Promise<void> {
    if (!this.isSupported) {
      throw new Error('Web Serial wird von diesem Browser nicht unterstuetzt (nur Chrome/Edge/Opera).');
    }

    const port = await navigator.serial.requestPort();
    await this.identifyPort(port);
  }

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

  private async identifyPort(port: SerialPort): Promise<void> {
    const id = await this.readDeviceId(port);
    if (id) {
      this.deviceIds.set(port, id);
      this.emitConnectedUnitIds();
      this.connectedUnitIdSubject.next(id);
    }
  }

  private emitConnectedUnitIds(): void {
    this.connectedUnitIdsSubject.next(Array.from(new Set(this.deviceIds.values())));
  }

  private async readDeviceId(port: SerialPort): Promise<string | null> {
    const state = await this.ensureReaderStarted(port);
    const responsePromise = this.waitForUuid(state, RESPONSE_TIMEOUT_MS);

    const writer = port.writable!.getWriter();
    try {
      console.log('[USB %s] write get_device_id', performance.now().toFixed(0));
      await writer.write(new TextEncoder().encode(`${IDENTIFY_COMMAND}\n`));
    } finally {
      writer.releaseLock();
    }

    const result = await responsePromise;
    console.log('[USB %s] readDeviceId resolved with', performance.now().toFixed(0), result);
    return result;
  }

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

    // Absichtlich nicht awaiten: läuft im Hintergrund für die gesamte
    // Lebensdauer des Ports weiter.
    this.pumpLines(port, state);

    return state;
  }

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
            const text = new TextDecoder().decode(new Uint8Array(bytes)).trim();
            bytes = [];

            if (text) {
              console.log('[USB %s] line:', performance.now().toFixed(0), JSON.stringify(text));

              // Erster passender Listener "verbraucht" die Zeile, alle
              // anderen bleiben fuer die naechste Zeile bestehen.
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
      // Port geschlossen/getrennt -> Hintergrund-Loop einfach beenden.
    } finally {
      reader.releaseLock();
      this.portReaders.delete(port);
    }
  }

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
