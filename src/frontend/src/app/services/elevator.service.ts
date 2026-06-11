import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Elevator, CreateReportDto } from '../models/elevator.model';

@Injectable({ providedIn: 'root' })
export class ElevatorService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/elevators';

  private readonly elevators = signal<Elevator[]>([]);
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);

  readonly elevatorsList = computed(() => this.elevators());
  readonly isLoading = computed(() => this.loading());
  readonly hasError = computed(() => this.error());

  loadElevators(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<Elevator[]>(this.apiUrl).subscribe({
      next: (data) => {
        this.elevators.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message);
        this.loading.set(false);
      }
    });
  }

  createReport(elevatorId: number, dto: CreateReportDto): void {
    this.http.post<Elevator>(`${this.apiUrl}/${elevatorId}/reports`, dto).subscribe({
      next: () => this.loadElevators(),
      error: (err) => this.error.set(err.message)
    });
  }
}