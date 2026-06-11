import { Component, input, output } from '@angular/core';
import { ElevatorStatus } from '../../models/elevator.model';

@Component({
  selector: 'app-report-dialog',
  imports: [],
  templateUrl: './report-dialog.html',
  styleUrl: './report-dialog.css'
})
export class ReportDialog {
  readonly elevatorId = input.required<number>();
  readonly elevatorName = input.required<string>();
  readonly reportSubmitted = output<{ elevatorId: number; status: ElevatorStatus }>();
  readonly closed = output<void>();

  selectStatus(status: ElevatorStatus): void {
    this.reportSubmitted.emit({ elevatorId: this.elevatorId(), status });
  }

  close(): void {
    this.closed.emit();
  }
}