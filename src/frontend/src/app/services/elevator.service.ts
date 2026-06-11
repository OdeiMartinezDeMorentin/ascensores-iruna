import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Elevator, CreateReportDto, UpdateReportDto, MyLatestReport } from '../models/elevator.model';

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

  createReport(elevatorId: number, dto: CreateReportDto): Promise<string | null> {
    return new Promise((resolve) => {
      this.http.post(`${this.apiUrl}/${elevatorId}/reports`, dto, { observe: 'response', responseType: 'json' })
        .subscribe({
          next: () => {
            this.loadElevators();
            resolve(null);
          },
          error: (err) => {
            if (err.status === 429) {
              resolve(err.error?.message || 'Has alcanzado el límite de reportes.');
            } else {
              resolve(err.message);
            }
          }
        });
    });
  }

  updateReport(elevatorId: number, dto: UpdateReportDto): Promise<string | null> {
    return new Promise((resolve) => {
      this.http.put(`${this.apiUrl}/${elevatorId}/reports/latest`, dto, { observe: 'response', responseType: 'json' })
        .subscribe({
          next: () => {
            this.loadElevators();
            resolve(null);
          },
          error: (err) => {
            if (err.status === 429) {
              resolve(err.error?.message || 'Has alcanzado el límite de reportes.');
            } else {
              resolve(err.message);
            }
          }
        });
    });
  }

  getMyLatestReport(elevatorId: number): Promise<MyLatestReport | null> {
    return new Promise((resolve) => {
      this.http.get<MyLatestReport>(`${this.apiUrl}/${elevatorId}/reports/my-latest`, { observe: 'response' })
        .subscribe({
          next: (response) => {
            if (response.status === 204 || !response.body) {
              resolve(null);
            } else {
              resolve(response.body);
            }
          },
          error: () => {
            resolve(null);
          }
        });
    });
  }
}