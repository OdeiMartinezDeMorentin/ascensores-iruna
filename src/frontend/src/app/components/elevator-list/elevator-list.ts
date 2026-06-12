import { Component, inject, output } from '@angular/core';
import { ElevatorService } from '../../services/elevator.service';
import { ElevatorCard } from '../elevator-card/elevator-card';

@Component({
  selector: 'app-elevator-list',
  imports: [ElevatorCard],
  templateUrl: './elevator-list.html',
  styleUrl: './elevator-list.css'
})
export class ElevatorList {
  private readonly elevatorService = inject(ElevatorService);

  readonly elevators = this.elevatorService.filteredElevators;
  readonly isLoading = this.elevatorService.isLoading;
  readonly hasError = this.elevatorService.hasError;

  readonly reportClicked = output<number>();
  readonly editClicked = output<number>();

  constructor() {
    this.elevatorService.loadElevators();
  }
}