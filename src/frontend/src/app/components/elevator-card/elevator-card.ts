import { Component, input, output, computed, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Elevator, RecentReport } from '../../models/elevator.model';
import { ElevatorService } from '../../services/elevator.service';

@Component({
  selector: 'app-elevator-card',
  imports: [],
  templateUrl: './elevator-card.html',
  styleUrl: './elevator-card.css'
})
export class ElevatorCard {
  private readonly sanitizer = inject(DomSanitizer);
  private readonly elevatorService = inject(ElevatorService);

  readonly elevator = input.required<Elevator>();
  readonly reportClicked = output<number>();
  readonly editClicked = output<number>();

  readonly showReports = signal(false);
  readonly reports = signal<RecentReport[]>([]);
  readonly loadingReports = signal(false);

  readonly statusClass = computed(() => {
    if (this.elevator().currentStatus === 'Desconocido') return 'desconocido';
    switch (this.elevator().currentStatus) {
      case 'NoOperativo': return 'no-operativo';
      default: return this.elevator().currentStatus.toLowerCase();
    }
  });

  private readonly statusIconHtml = computed(() => {
    switch (this.elevator().currentStatus) {
      case 'Operativo':
        return '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#28a745" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/></svg>';
      case 'NoOperativo':
        return '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#dc3545" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m15 9-6 6"/><path d="m9 9 6 6"/></svg>';
      default:
        return '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#6c757d" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>';
    }
  });

  readonly statusIcon = computed<SafeHtml>(() => this.sanitizer.bypassSecurityTrustHtml(this.statusIconHtml()));

  readonly statusLabel = computed(() => {
    if (this.elevator().currentStatus === 'Desconocido') return 'Sin reportes';
    const baseLabel = this.elevator().currentStatus === 'Operativo' ? 'Operativo' : 'No operativo';
    return baseLabel;
  });

  readonly hasConflict = computed(() => this.elevator().hasConflict && this.elevator().currentStatus !== 'Desconocido');

  readonly timeAgo = computed(() => {
    const reportedAt = this.elevator().lastReportedAt;
    if (!reportedAt) return null;

    const now = new Date().getTime();
    const reported = new Date(reportedAt).getTime();
    const diffMs = now - reported;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'ahora mismo';
    if (diffMins < 60) return `hace ${diffMins} min`;
    if (diffHours < 24) return `hace ${diffHours}h`;
    return `hace ${diffDays}d`;
  });

  async toggleReports(): Promise<void> {
    if (this.showReports()) {
      this.showReports.set(false);
      return;
    }

    this.loadingReports.set(true);
    this.showReports.set(true);
    const data = await this.elevatorService.getRecentReports(this.elevator().id);
    this.reports.set(data);
    this.loadingReports.set(false);
  }

  formatTimeAgo(dateStr: string): string {
    const now = new Date().getTime();
    const reported = new Date(dateStr).getTime();
    const diffMs = now - reported;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'ahora mismo';
    if (diffMins < 60) return `hace ${diffMins} min`;
    if (diffHours < 24) return `hace ${diffHours}h`;
    if (diffDays < 7) return `hace ${diffDays}d`;
    const diffWeeks = Math.floor(diffDays / 7);
    if (diffWeeks <= 4) return `hace ${diffWeeks} semana${diffWeeks === 1 ? '' : 's'}`;
    return 'hace más de un mes';
  }
}