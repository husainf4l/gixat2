import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { LogoComponent } from '../../components/logo/logo.component';

interface RepairStage {
  id: number;
  label: string;
  time: string;
  status: 'completed' | 'active' | 'pending';
  icon: string;
}

interface Advisor {
  name: string;
  role: string;
  image: string;
}

interface Quote {
  labor: number;
  parts: number;
  tax: number;
  total: number;
}

interface RepairData {
  customerName: string;
  vehicle: string;
  plate: string;
  vin: string;
  status: string;
  progress: number;
  estimatedReady: string;
  advisor: Advisor;
  aiInsight: string;
  stages: RepairStage[];
  quote: Quote;
}

@Component({
  selector: 'app-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, LogoComponent],
  templateUrl: './tracking.component.html'
})
export class TrackingComponent {
  private router = inject(Router);

  plate = signal('');
  phone = signal('');
  isSearching = signal(false);
  repairData = signal<RepairData | null>(null);
  error = signal<string | null>(null);

  // Mock data for demo
  private mockRepairData: Record<string, RepairData> = {
    'DEMO-777': {
      customerName: 'Jordan Smith',
      vehicle: 'Porsche Taycan Turbo S',
      plate: 'DEMO-777',
      vin: 'WP0ZZZ91ZLS12XXXX',
      status: 'In Progress',
      progress: 68,
      estimatedReady: 'Today, 5:15 PM',
      advisor: {
        name: 'Steve Rogers',
        role: 'Lead Service Advisor',
        image: 'https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&q=80&w=100&h=100'
      },
      aiInsight: 'Battery thermal management system requires sensor recalibration. Front-left control arm bushing shows premature wear (Grade B). Overall mechanical health is excellent.',
      stages: [
        { id: 1, label: 'Vehicle Arrived', time: '08:45 AM', status: 'completed', icon: 'ri-map-pin-2-line' },
        { id: 2, label: 'Gixat AI Diagnostics', time: '09:20 AM', status: 'completed', icon: 'ri-shield-flash-line' },
        { id: 3, label: 'Precision Repair', time: '10:45 AM', status: 'active', icon: 'ri-tools-line' },
        { id: 4, label: 'Quality Verification', time: '--', status: 'pending', icon: 'ri-checkbox-circle-line' },
        { id: 5, label: 'Ready for Pickup', time: '--', status: 'pending', icon: 'ri-flag-2-line' },
      ],
      quote: {
        labor: 145.00,
        parts: 289.50,
        tax: 34.75,
        total: 469.25
      }
    },
    'ABC-123': {
      customerName: 'Alice Cooper',
      vehicle: 'Toyota Camry 2021',
      plate: 'ABC-123',
      vin: '4T1BD1FK1LU02XXXX',
      status: 'In Progress',
      progress: 68,
      estimatedReady: 'Today, 5:15 PM',
      advisor: {
        name: 'Steve Rogers',
        role: 'Lead Service Advisor',
        image: 'https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&q=80&w=100&h=100'
      },
      aiInsight: 'Brake friction material measured at 3mm. Recommend replacement of front rotors due to thermal warping detected during diagnostic drive. Brake fluid moisture content: 1.2% (Safe).',
      stages: [
        { id: 1, label: 'Vehicle Arrived', time: '08:45 AM', status: 'completed', icon: 'ri-map-pin-2-line' },
        { id: 2, label: 'Gixat AI Diagnostics', time: '09:20 AM', status: 'completed', icon: 'ri-shield-flash-line' },
        { id: 3, label: 'Precision Repair', time: '10:45 AM', status: 'active', icon: 'ri-tools-line' },
        { id: 4, label: 'Quality Verification', time: '--', status: 'pending', icon: 'ri-checkbox-circle-line' },
        { id: 5, label: 'Ready for Pickup', time: '--', status: 'pending', icon: 'ri-flag-2-line' },
      ],
      quote: {
        labor: 145.00,
        parts: 289.50,
        tax: 34.75,
        total: 469.25
      }
    }
  };

  handleTrack(demoPlate?: string) {
    const searchPlate = demoPlate || this.plate().toUpperCase().trim();
    const searchPhone = demoPlate ? '555-0100' : this.phone().trim();

    if (!searchPlate || !searchPhone) {
      this.error.set('Please provide both plate and phone number to access the portal.');
      return;
    }

    this.error.set(null);
    this.isSearching.set(true);
    
    // Simulate API call
    setTimeout(() => {
      const upPlate = searchPlate.toUpperCase();
      if (upPlate.includes('ABC') || upPlate === 'DEMO-777') {
        const plateKey = upPlate === 'DEMO-777' ? 'DEMO-777' : 'ABC-123';
        this.repairData.set(this.mockRepairData[plateKey]);
      } else {
        this.error.set('No active repair found. Please verify your plate number or contact your workshop advisor.');
      }
      this.isSearching.set(false);
    }, 1500);
  }

  runDemo() {
    this.plate.set('DEMO-777');
    this.phone.set('555-0100');
    this.handleTrack('DEMO-777');
  }

  onBack() {
    this.router.navigate(['/']);
  }

  resetTracking() {
    this.repairData.set(null);
    this.plate.set('');
    this.phone.set('');
    this.error.set(null);
  }
}

