import { Component, input, output, computed } from '@angular/core';
import { Elevator } from '../../models/elevator.model';

@Component({
  selector: 'app-elevator-card',
  imports: [],
  templateUrl: './elevator-card.html',
  styleUrl: './elevator-card.css'
})
export class ElevatorCard {
  readonly elevator = input.required<Elevator>();
  readonly reportClicked = output<number>();
  readonly editClicked = output<number>();

  readonly statusIcon = computed(() => {
    switch (this.elevator().currentStatus) {
      case 'Operativo': return '\uD83D\uDFE2';
      case 'Parcial': return '\uD83D\uDFE1';
      case 'Averiado': return '\uD83D\uDD34';
      default: return '\u26AA';
    }
  });

  readonly statusLabel = computed(() => {
    switch (this.elevator().currentStatus) {
      case 'Operativo': return 'Operativo';
      case 'Parcial': return 'Parcialmente operativo';
      case 'Averiado': return 'Averiado';
      default: return 'Sin reportes';
    }
  });

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
}