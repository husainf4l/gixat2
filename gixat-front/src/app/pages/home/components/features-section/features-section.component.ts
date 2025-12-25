import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-features-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './features-section.component.html'
})
export class FeaturesSectionComponent {
  selectedFeature = signal(0);

  features = [
    {
      id: 1,
      icon: 'ri-calendar-event-line',
      iconColor: 'text-[#0071e3]',
      iconBg: 'bg-blue-50',
      title: 'Smart Scheduling',
      shortDesc: '24/7 online booking with automated reminders',
      description: 'Customers book online 24/7. Automatic SMS and email reminders cut no-shows by 80%. Your schedule stays full, your team stays productive.',
      previewImage: '/hero/feature_scheduling.png',
      highlights: [
        'Real-time availability sync',
        'Automated SMS & email reminders',
        'Multi-location calendar management',
        'Customer self-service booking'
      ]
    },
    {
      id: 2,
      icon: 'ri-file-list-3-line',
      iconColor: 'text-purple-600',
      iconBg: 'bg-purple-50',
      title: 'Digital Job Cards',
      shortDesc: 'Track every job from start to finish',
      description: 'Digital job cards replace messy paper trails. Technicians update progress in real-time. Customers see exactly what\'s being done and why.',
      previewImage: '/hero/feature_jobcards.png',
      highlights: [
        'Real-time progress tracking',
        'Photo documentation',
        'Instant estimates & approvals',
        'Digital signatures'
      ]
    },
    {
      id: 3,
      icon: 'ri-user-3-line',
      iconColor: 'text-amber-600',
      iconBg: 'bg-amber-50',
      title: 'Customer Management',
      shortDesc: 'Complete service history at your fingertips',
      description: 'Complete service history for every customer and vehicle. No more "When was my last oil change?" calls. Build trust with every visit.',
      previewImage: '/hero/heroo8.webp',
      highlights: [
        'Full service history',
        'Multi-vehicle tracking',
        'Customer preferences & notes',
        'Automated follow-ups'
      ]
    },
    {
      id: 4,
      icon: 'ri-inbox-line',
      iconColor: 'text-emerald-600',
      iconBg: 'bg-emerald-50',
      title: 'Inventory Control',
      shortDesc: 'Never run out of critical parts',
      description: 'Get alerts before you run low. Track what sells fast. Know exactly what\'s in stock without walking to the storage room.',
      previewImage: '/hero/heroo9.webp',
      highlights: [
        'Low-stock alerts',
        'Real-time inventory levels',
        'Supplier management',
        'Usage analytics'
      ]
    },
    {
      id: 5,
      icon: 'ri-team-line',
      iconColor: 'text-indigo-600',
      iconBg: 'bg-indigo-50',
      title: 'Team Collaboration',
      shortDesc: 'Keep everyone on the same page',
      description: 'Assign jobs to technicians. Track who\'s working on what. No more shouting across the shop floor to find out job status.',
      previewImage: '/hero/heroo10.webp',
      highlights: [
        'Smart task assignment',
        'Role-based permissions',
        'Team performance tracking',
        'Internal messaging'
      ]
    },
    {
      id: 6,
      icon: 'ri-bar-chart-line',
      iconColor: 'text-rose-600',
      iconBg: 'bg-rose-50',
      title: 'Business Analytics',
      shortDesc: 'Data-driven decisions made easy',
      description: 'Revenue trends, top services, customer retention—all in simple dashboards. Make smart decisions based on real data, not gut feelings.',
      previewImage: '/hero/gixathero-1.webp',
      highlights: [
        'Revenue tracking',
        'Custom reports',
        'Performance metrics',
        'One-click exports'
      ]
    }
  ];

  selectFeature(index: number) {
    this.selectedFeature.set(index);
  }
}
