import { Component, output } from '@angular/core';

@Component({
  selector: 'app-welcome-dialog',
  imports: [],
  templateUrl: './welcome-dialog.html',
  styleUrl: './welcome-dialog.css'
})
export class WelcomeDialog {
  readonly closed = output<void>();

  close(): void {
    this.closed.emit();
  }
}