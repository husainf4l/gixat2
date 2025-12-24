import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-data-deletion',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './data-deletion.component.html',
})
export class DataDeletionComponent {
  lastUpdated = 'December 24, 2024';
}

