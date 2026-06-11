export interface Elevator {
  id: number;
  name: string;
  location: string;
  latitude: number;
  longitude: number;
  currentStatus: string;
  lastReportedAt: string | null;
  totalReports: number;
}

export type ElevatorStatus = 'Operativo' | 'Parcial' | 'Averiado' | 'Desconocido';

export interface CreateReportDto {
  status: string;
}