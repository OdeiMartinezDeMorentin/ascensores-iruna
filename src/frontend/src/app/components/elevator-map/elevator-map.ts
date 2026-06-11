import { Component, effect, inject, output, ElementRef, ViewChild } from '@angular/core';
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

  readonly reportClicked = output<number>();

  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLElement>;
  private map: L.Map | null = null;
  private markers: L.Marker[] = [];
  private pinnedMarker: L.Marker | null = null;
  private hoverTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const elevators = this.elevatorService.elevatorsList();
      if (elevators.length > 0) {
        this.updateMarkers(elevators);
      }
    });
  }

  ngAfterViewInit(): void {
    this.initMap();
    const elevators = this.elevatorService.elevatorsList();
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
      zoomControl: true
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(this.map);

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
      const marker = L.marker([elevator.latitude, elevator.longitude], { icon })
        .addTo(this.map)
        .bindPopup(this.createPopupContent(elevator), { closeButton: true });

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
    const colorMap: Record<string, string> = {
      'Operativo': '#28a745',
      'Parcial': '#ffc107',
      'Averiado': '#dc3545',
      'Desconocido': '#6c757d'
    };
    const color = colorMap[status] ?? '#6c757d';

    return L.divIcon({
      className: 'elevator-marker',
      html: `<div style="
        background:${color};
        width:24px;height:24px;
        border-radius:50%;
        border:3px solid #fff;
        box-shadow:0 2px 6px rgba(0,0,0,0.4);
      "></div>`,
      iconSize: [24, 24],
      iconAnchor: [12, 12],
      popupAnchor: [0, -14]
    });
  }

  private createPopupContent(elevator: Elevator): string {
    const statusMap: Record<string, string> = {
      'Operativo': '🟢 Operativo',
      'Parcial': '🟡 Parcialmente operativo',
      'Averiado': '🔴 Averiado',
      'Desconocido': '⚪ Sin reportes'
    };
    const statusLabel = statusMap[elevator.currentStatus] ?? '⚪ Sin reportes';
    const escapedName = elevator.name.replace(/'/g, '&#39;').replace(/"/g, '&quot;');
    const escapedLocation = elevator.location.replace(/'/g, '&#39;').replace(/"/g, '&quot;');

    return `
      <div style="font-family:Inter,-apple-system,sans-serif;min-width:180px">
        <strong style="font-size:1rem;color:#1a1a2e">${escapedName}</strong>
        <p style="margin:2px 0 4px;color:#6c757d;font-size:0.85rem">${escapedLocation}</p>
        <p style="margin:0 0 8px;font-size:0.9rem;font-weight:600">${statusLabel}</p>
        <button
          onclick="document.dispatchEvent(new CustomEvent('report-elevator',{detail:${elevator.id}}))"
          style="
            width:100%;padding:6px 0;
            border:1px solid #dee2e6;border-radius:8px;
            background:#f8f9fa;color:#495057;
            font-size:0.85rem;cursor:pointer;
          ">
          Reportar estado
        </button>
      </div>`;
  }
}