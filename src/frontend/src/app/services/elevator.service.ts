import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Elevator, CreateReportDto, UpdateReportDto, MyLatestReport } from '../models/elevator.model';

const SITE_GROUPS: string[][] = [
  ['Descalzos'],
  ['Concepción Benítez', 'Concepcion Benitez']
];

function getGroupKey(name: string): string | null {
  const lower = name.toLowerCase();
  for (const keywords of SITE_GROUPS) {
    if (keywords.some(k => lower.includes(k.toLowerCase()))) {
      return keywords[0];
    }
  }
  return null;
}

@Injectable({ providedIn: 'root' })
export class ElevatorService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/elevators';

  private readonly elevators = signal<Elevator[]>([]);
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);
  readonly searchTerm = signal('');

  readonly elevatorsList = computed(() => this.elevators());
  readonly filteredElevators = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const source = this.elevators();
    const filtered = term
      ? source.filter(e => e.name.toLowerCase().includes(term) || e.location.toLowerCase().includes(term))
      : source;

    const groupTotals = new Map<string, { sum: number; count: number }>();
    for (const e of filtered) {
      const key = getGroupKey(e.name);
      if (key) {
        const prev = groupTotals.get(key) ?? { sum: 0, count: 0 };
        groupTotals.set(key, { sum: prev.sum + e.totalReports, count: prev.count + 1 });
      }
    }

    const getSortScore = (e: Elevator): number => {
      const key = getGroupKey(e.name);
      if (!key) return e.totalReports;
      const g = groupTotals.get(key)!;
      return g.count > 0 ? g.sum / g.count : e.totalReports;
    };

    return [...filtered].sort((a, b) => {
      const scoreA = getSortScore(a);
      const scoreB = getSortScore(b);
      if (scoreA !== scoreB) return scoreB - scoreA;
      const keyA = getGroupKey(a.name);
      const keyB = getGroupKey(b.name);
      if (keyA && keyA === keyB) return a.id - b.id;
      return 0;
    });
  });
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
              const msg = typeof err.error === 'string' ? err.error : err.error?.message;
              resolve(msg || 'Has alcanzado el máximo de reportes, espera 10 minutos.');
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
              const msg = typeof err.error === 'string' ? err.error : err.error?.message;
              resolve(msg || 'Has alcanzado el máximo de reportes, espera 10 minutos.');
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

  deleteReport(elevatorId: number): Promise<string | null> {
    return new Promise((resolve) => {
      this.http.delete(`${this.apiUrl}/${elevatorId}/reports/latest`, { observe: 'response', responseType: 'json' })
        .subscribe({
          next: () => {
            this.loadElevators();
            resolve(null);
          },
          error: (err) => {
            resolve(err.message || 'Error al anular el reporte.');
          }
        });
    });
  }
}