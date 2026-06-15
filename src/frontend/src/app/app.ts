import { Component, inject, signal } from '@angular/core';
import { ElevatorList } from './components/elevator-list/elevator-list';
import { ElevatorMap } from './components/elevator-map/elevator-map';
import { ReportDialog } from './components/report-dialog/report-dialog';
import { InfoDialog } from './components/info-dialog/info-dialog';
import { ElevatorService } from './services/elevator.service';
import { ElevatorStatus } from './models/elevator.model';

@Component({
  selector: 'app-root',
  imports: [ElevatorMap, ElevatorList, ReportDialog, InfoDialog],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly elevatorService = inject(ElevatorService);

  readonly showDialog = signal(false);
  readonly selectedElevatorId = signal(0);
  readonly selectedElevatorName = signal('');
  readonly isEditing = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showInfoDialog = signal(false);
  readonly mapFullscreen = signal(false);
  readonly searchTerm = this.elevatorService.searchTerm;

  toggleMapFullscreen(): void {
    this.mapFullscreen.update(v => !v);
  }

  openReportDialog(elevatorId: number): void {
    const elevator = this.elevatorService.elevatorsList().find(e => e.id === elevatorId);
    if (elevator) {
      this.selectedElevatorId.set(elevatorId);
      this.selectedElevatorName.set(elevator.name);
      this.isEditing.set(!elevator.canReport);
      this.errorMessage.set(null);
      this.showDialog.set(true);
    }
  }

  openEditDialog(elevatorId: number): void {
    const elevator = this.elevatorService.elevatorsList().find(e => e.id === elevatorId);
    if (elevator) {
      this.selectedElevatorId.set(elevatorId);
      this.selectedElevatorName.set(elevator.name);
      this.isEditing.set(true);
      this.errorMessage.set(null);
      this.showDialog.set(true);
    }
  }

  async onReportSubmitted(event: { elevatorId: number; status: ElevatorStatus }): Promise<void> {
    this.errorMessage.set(null);
    let error: string | null;

    if (this.isEditing()) {
      error = await this.elevatorService.updateReport(event.elevatorId, { status: event.status });
    } else {
      error = await this.elevatorService.createReport(event.elevatorId, { status: event.status });
    }

    if (error) {
      this.errorMessage.set(error);
    } else {
      this.showDialog.set(false);
    }
  }

  onDialogClosed(): void {
    this.showDialog.set(false);
    this.errorMessage.set(null);
  }

  async onReportDeleted(elevatorId: number): Promise<void> {
    const error = await this.elevatorService.deleteReport(elevatorId);
    if (error) {
      this.errorMessage.set(error);
    } else {
      this.showDialog.set(false);
    }
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.elevatorService.searchTerm.set(value);
  }

  clearSearch(): void {
    this.elevatorService.searchTerm.set('');
  }
}