import { Component, inject, signal } from '@angular/core';
import { ElevatorList } from './components/elevator-list/elevator-list';
import { ReportDialog } from './components/report-dialog/report-dialog';
import { ElevatorService } from './services/elevator.service';
import { ElevatorStatus } from './models/elevator.model';

@Component({
  selector: 'app-root',
  imports: [ElevatorList, ReportDialog],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly elevatorService = inject(ElevatorService);

  readonly showDialog = signal(false);
  readonly selectedElevatorId = signal(0);
  readonly selectedElevatorName = signal('');

  openReportDialog(elevatorId: number): void {
    const elevator = this.elevatorService.elevatorsList().find(e => e.id === elevatorId);
    if (elevator) {
      this.selectedElevatorId.set(elevatorId);
      this.selectedElevatorName.set(elevator.name);
      this.showDialog.set(true);
    }
  }

  onReportSubmitted(event: { elevatorId: number; status: ElevatorStatus }): void {
    this.elevatorService.createReport(event.elevatorId, { status: event.status });
    this.showDialog.set(false);
  }

  onDialogClosed(): void {
    this.showDialog.set(false);
  }
}