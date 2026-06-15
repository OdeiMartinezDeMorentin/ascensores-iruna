import { Component, effect, inject, output, ElementRef, ViewChild, input } from '@angular/core';
import * as L from 'leaflet';
import { ElevatorService } from '../../services/elevator.service';
import { Elevator } from '../../models/elevator.model';

@Component({
  selector: 'app-elevator-map',
  imports: [],
  templateUrl: './elevator-map.html',
  styleUrl: './elevator-map.css'
})
export class ElevatorMap {
  private readonly elevatorService = inject(ElevatorService);

  readonly fullscreen = input(false);
  readonly reportClicked = output<number>();
  readonly fullscreenToggled = output<void>();

  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLElement>;
  private map: L.Map | null = null;
  private markers: L.Marker[] = [];
  private pinnedMarker: L.Marker | null = null;
  private hoverTimeout: ReturnType<typeof setTimeout> | null = null;
  private readonly isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

  constructor() {
    effect(() => {
      const elevators = this.elevatorService.filteredElevators();
      if (elevators.length > 0) {
        this.updateMarkers(elevators);
      }
    });

    effect(() => {
      const fs = this.fullscreen();
      if (this.map) {
        setTimeout(() => this.map?.invalidateSize(), 50);
      }
    });
  }

  ngAfterViewInit(): void {
    this.initMap();
    const elevators = this.elevatorService.filteredElevators();
    if (elevators.length > 0) {
      this.updateMarkers(elevators);
    }
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  private initMap(): void {
    this.map = L.map(this.mapContainer.nativeElement, {
      center: [42.8168, -1.6488],
      zoom: 14,
      zoomControl: !this.isTouchDevice,
      tapTolerance: 15
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(this.map);

    if (this.isTouchDevice) {
      L.control.zoom({ position: 'bottomright' }).addTo(this.map);
    }

    this.map.on('click', () => {
      if (this.pinnedMarker) {
        this.pinnedMarker.closePopup();
        this.pinnedMarker = null;
      }
    });
  }

  private updateMarkers(elevators: Elevator[]): void {
    if (!this.map) return;

    this.markers.forEach(m => m.remove());
    this.markers = [];

    for (const elevator of elevators) {
      const icon = this.createIcon(elevator.currentStatus);
      const popupContent = this.createPopupContent(elevator);
      const marker = L.marker([elevator.latitude, elevator.longitude], { icon })
        .addTo(this.map)
        .bindPopup(popupContent, {
          closeButton: true,
          maxWidth: 260,
          autoPan: true,
          autoPanPaddingTopLeft: L.point(10, 10),
          autoPanPaddingBottomRight: L.point(10, 10)
        });

      if (!this.isTouchDevice) {
        marker.on('mouseover', () => {
          if (this.hoverTimeout) {
            clearTimeout(this.hoverTimeout);
            this.hoverTimeout = null;
          }
          if (this.pinnedMarker && this.pinnedMarker !== marker) {
            this.pinnedMarker.closePopup();
            this.pinnedMarker = null;
          }
          marker.openPopup();
        });

        marker.on('mouseout', () => {
          if (this.pinnedMarker === marker) return;
          this.scheduleClose(marker);
        });

        marker.on('popupopen', () => {
          const popupEl = marker.getPopup()?.getElement();
          if (popupEl) {
            popupEl.addEventListener('mouseenter', () => {
              if (this.hoverTimeout) {
                clearTimeout(this.hoverTimeout);
                this.hoverTimeout = null;
              }
            });
            popupEl.addEventListener('mouseleave', () => {
              if (this.pinnedMarker === marker) return;
              this.scheduleClose(marker);
            });
          }
        });
      }

      marker.on('click', () => {
        if (this.hoverTimeout) {
          clearTimeout(this.hoverTimeout);
          this.hoverTimeout = null;
        }
        if (this.pinnedMarker === marker) {
          this.pinnedMarker.closePopup();
          this.pinnedMarker = null;
        } else {
          if (this.pinnedMarker) {
            this.pinnedMarker.closePopup();
          }
          this.pinnedMarker = marker;
          marker.openPopup();
        }
      });

      marker.on('popupclose', () => {
        if (this.pinnedMarker === marker) {
          this.pinnedMarker = null;
        }
      });

      this.markers.push(marker);
    }
  }

  private scheduleClose(marker: L.Marker): void {
    this.hoverTimeout = setTimeout(() => {
      marker.closePopup();
      this.hoverTimeout = null;
    }, 200);
  }

  private createIcon(status: string): L.DivIcon {
    const icons: Record<string, string> = {
      'Operativo': `<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36" viewBox="0 0 24 24" fill="#28a745" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" fill="#28a745" stroke="#fff" stroke-width="2"/><path d="m9 12 2 2 4-4" stroke="#fff" stroke-width="2.5" fill="none"/></svg>`,
      'Parcial': `<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36" viewBox="0 0 24 24" fill="#ffc107" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" fill="#ffc107" stroke="#fff" stroke-width="2"/><line x1="12" y1="8" x2="12" y2="12" stroke="#fff" stroke-width="2.5"/><line x1="12" y1="16" x2="12.01" y2="16" stroke="#fff" stroke-width="2.5"/></svg>`,
      'NoOperativo': `<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36" viewBox="0 0 24 24" fill="#dc3545" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" fill="#dc3545" stroke="#fff" stroke-width="2"/><path d="m15 9-6 6" stroke="#fff" stroke-width="2.5"/><path d="m9 9 6 6" stroke="#fff" stroke-width="2.5"/></svg>`,
      'Desconocido': `<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36" viewBox="0 0 24 24" fill="#6c757d" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" fill="#6c757d" stroke="#fff" stroke-width="2"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" stroke="#fff" stroke-width="2" fill="none"/><line x1="12" y1="17" x2="12.01" y2="17" stroke="#fff" stroke-width="2.5"/></svg>`
    };
    const svg = icons[status] ?? icons['Desconocido'];

    return L.divIcon({
      className: 'elevator-marker',
      html: `<div style="width:36px;height:36px;filter:drop-shadow(0 2px 4px rgba(0,0,0,0.4))">${svg}</div>`,
      iconSize: [36, 36],
      iconAnchor: [18, 18],
      popupAnchor: [0, -20]
    });
  }

  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private createPopupContent(elevator: Elevator): HTMLElement {
    const statusIcons: Record<string, string> = {
      'Operativo': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#28a745" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/></svg>`,
      'Parcial': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#ffc107" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>`,
      'NoOperativo': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#dc3545" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m15 9-6 6"/><path d="m9 9 6 6"/></svg>`,
      'Desconocido': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#6c757d" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`
    };
    const statusLabels: Record<string, string> = {
      'Operativo': 'Operativo',
      'Parcial': 'Reportes contradictorios',
      'NoOperativo': 'No operativo',
      'Desconocido': 'Sin reportes'
    };
    const icon = statusIcons[elevator.currentStatus] ?? statusIcons['Desconocido'];
    const label = statusLabels[elevator.currentStatus] ?? 'Sin reportes';
    const escapedName = this.escapeHtml(elevator.name);
    const escapedLocation = this.escapeHtml(elevator.location);

    const btnText = elevator.canReport ? 'Reportar estado' : 'Modificar reporte';
    const btnStyle = elevator.canReport
      ? 'width:100%;padding:12px 0;border:1px solid #dee2e6;border-radius:8px;background:#f8f9fa;color:#495057;font-size:0.95rem;cursor:pointer;min-height:44px;'
      : 'width:100%;padding:12px 0;border:1px solid #ffc107;border-radius:8px;background:#fff8e1;color:#856404;font-size:0.95rem;cursor:pointer;min-height:44px;';

    const container = document.createElement('div');
    container.style.fontFamily = 'Inter,-apple-system,sans-serif';
    container.style.minWidth = '160px';
    container.innerHTML = `
      <strong style="font-size:1rem;color:#1a1a2e">${escapedName}</strong>
      <p style="margin:2px 0 4px;color:#6c757d;font-size:0.85rem">${escapedLocation}</p>
      <p style="margin:0 0 8px;font-size:0.9rem;font-weight:600;display:flex;align-items:center;gap:4px">${icon} ${label}</p>
      <button style="${btnStyle}">${btnText}</button>
    `;

    const button = container.querySelector('button');
    if (button) {
      L.DomEvent.on(button, 'click', (e: Event) => {
        L.DomEvent.stopPropagation(e);
        L.DomEvent.preventDefault(e);
        this.map?.closePopup();
        this.reportClicked.emit(elevator.id);
      });
    }

    return container;
  }
}