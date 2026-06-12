import { Component, input, output, computed, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ElevatorStatus } from '../../models/elevator.model';

@Component({
  selector: 'app-report-dialog',
  imports: [],
  templateUrl: './report-dialog.html',
  styleUrl: './report-dialog.css'
})
export class ReportDialog {
  private readonly sanitizer = inject(DomSanitizer);

  readonly elevatorId = input.required<number>();
  readonly elevatorName = input.required<string>();
  readonly isEditing = input<boolean>();
  readonly errorMessage = input<string | null>();
  readonly reportSubmitted = output<{ elevatorId: number; status: ElevatorStatus }>();
  readonly reportDeleted = output<number>();
  readonly closed = output<void>();

  private readonly operativoSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#28a745" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/></svg>';
  private readonly noOperativoSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#dc3545" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m15 9-6 6"/><path d="m9 9 6 6"/></svg>';

  readonly operativoIcon = computed<SafeHtml>(() => this.sanitizer.bypassSecurityTrustHtml(this.operativoSvg));
  readonly noOperativoIcon = computed<SafeHtml>(() => this.sanitizer.bypassSecurityTrustHtml(this.noOperativoSvg));

  selectStatus(status: ElevatorStatus): void {
    this.reportSubmitted.emit({ elevatorId: this.elevatorId(), status });
  }

  deleteReport(): void {
    this.reportDeleted.emit(this.elevatorId());
  }

  close(): void {
    this.closed.emit();
  }
}