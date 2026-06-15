import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-toast',
  imports: [],
  templateUrl: './toast.html',
  styleUrl: './toast.css'
})
export class Toast {
  readonly message = signal<string | null>(null);
  private timer: ReturnType<typeof setTimeout> | null = null;

  show(msg: string, duration = 3000): void {
    if (this.timer) {
      clearTimeout(this.timer);
    }
    this.message.set(msg);
    this.timer = setTimeout(() => {
      this.message.set(null);
      this.timer = null;
    }, duration);
  }

  clear(): void {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
    this.message.set(null);
  }
}