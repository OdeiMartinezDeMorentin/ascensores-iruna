export interface Elevator {
  id: number;
  name: string;
  location: string;
  latitude: number;
  longitude: number;
  currentStatus: string;
  hasConflict: boolean;
  lastReportedAt: string | null;
  totalReports: number;
  recentReports: number;
  canReport: boolean;
}

export type ElevatorStatus = 'Operativo' | 'Parcial' | 'NoOperativo' | 'Desconocido';

export interface CreateReportDto {
  status: string;
}

export interface UpdateReportDto {
  status: string;
}

export interface MyLatestReport {
  reportId: number;
  status: string;
  reportedAt: string;
  canEdit: boolean;
}

export interface RecentReport {
  status: string;
  reportedAt: string;
}