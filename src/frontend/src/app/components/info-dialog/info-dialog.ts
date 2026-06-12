import { Component, output } from '@angular/core';

@Component({
  selector: 'app-info-dialog',
  imports: [],
  templateUrl: './info-dialog.html',
  styleUrl: './info-dialog.css'
})
export class InfoDialog {
  readonly closed = output<void>();

  close(): void {
    this.closed.emit();
  }
}