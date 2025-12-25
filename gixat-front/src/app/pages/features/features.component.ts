import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-features',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './features.component.html',
  styleUrl: './features.component.css'
})
export class FeaturesComponent {
  selectedFeature = signal(0);

  features = [
    {
      id: 1,
      icon: 'ri-calendar-event-line',
      title: 'Smart Scheduling',
      tagline: 'Always available',
      description: 'Customers book online 24/7. Automatic SMS and email reminders cut no-shows by 80%. Your schedule stays full, your team stays productive.',
      previewImage: '/hero/feature_scheduling.png',
      highlights: [
        'Real-time availability sync',
        'Automated SMS & email reminders',
        'Multi-location calendar management',
        'Customer self-service booking'
      ],
      color: 'blue'
    },
    {
      id: 2,
      icon: 'ri-file-list-3-line',
      title: 'Digital Job Cards',
      tagline: 'Track everything',
      description: 'Digital job cards replace messy paper trails. Technicians update progress in real-time. Customers see exactly what\'s being done and why.',
      previewImage: '/hero/feature_jobcards.png',
      highlights: [
        'Real-time progress tracking',
        'Photo documentation',
        'Instant estimates & approvals',
        'Digital signatures'
      ],
      color: 'purple'
    },
    {
      id: 3,
      icon: 'ri-user-3-line',
      title: 'Customer Management',
      tagline: 'Complete history',
      description: 'Complete service history for every customer and vehicle. No more "When was my last oil change?" calls. Build trust with every visit.',
      previewImage: '/hero/heroo8.webp',
      highlights: [
        'Full service history',
        'Multi-vehicle tracking',
        'Customer preferences & notes',
        'Automated follow-ups'
      ],
      color: 'amber'
    },
    {
      id: 4,
      icon: 'ri-inbox-line',
      title: 'Inventory Control',
      tagline: 'Never run out',
      description: 'Get alerts before you run low. Track what sells fast. Know exactly what\'s in stock without walking to the storage room.',
      previewImage: '/hero/heroo9.webp',
      highlights: [
        'Low-stock alerts',
        'Real-time inventory levels',
        'Supplier management',
        'Usage analytics'
      ],
      color: 'emerald'
    },
    {
      id: 5,
      icon: 'ri-team-line',
      title: 'Team Collaboration',
      tagline: 'Stay connected',
      description: 'Assign jobs to technicians. Track who\'s working on what. No more shouting across the shop floor to find out job status.',
      previewImage: '/hero/heroo10.webp',
      highlights: [
        'Smart task assignment',
        'Role-based permissions',
        'Team performance tracking',
        'Internal messaging'
      ],
      color: 'indigo'
    },
    {
      id: 6,
      icon: 'ri-bar-chart-line',
      title: 'Business Analytics',
      tagline: 'Data-driven decisions',
      description: 'Revenue trends, top services, customer retention—all in simple dashboards. Make smart decisions based on real data, not gut feelings.',
      previewImage: '/hero/gixathero-1.webp',
      highlights: [
        'Revenue tracking',
        'Custom reports',
        'Performance metrics',
        'One-click exports'
      ],
      color: 'rose'
    }
  ];

  selectFeature(index: number) {
    this.selectedFeature.set(index);
  }
}

