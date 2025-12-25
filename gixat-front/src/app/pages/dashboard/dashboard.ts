import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  user = this.authService.currentUser;
  currentDate = new Date().toLocaleDateString('en-US', { 
    weekday: 'long', 
    year: 'numeric', 
    month: 'long', 
    day: 'numeric' 
  });

  // Stats Data
  stats = [
    { 
      title: 'Daily Revenue', 
      value: '$2,480', 
      subValue: 'vs $1.9k avg',
      trend: '+22%', 
      trendType: 'up',
      icon: 'ri-line-chart-line', 
      color: 'bg-blue-50 text-[#1b75bc]',
      trendColor: 'text-emerald-700 bg-emerald-50'
    },
    { 
      title: 'Active Vehicles', 
      value: '14', 
      subValue: '4 bays free',
      trend: '+2', 
      trendType: 'up',
      icon: 'ri-tools-line', 
      color: 'bg-amber-50 text-amber-700',
      trendColor: 'text-emerald-700 bg-emerald-50'
    },
    { 
      title: 'Completed Today', 
      value: '8', 
      subValue: 'Goal: 10',
      trend: '80%', 
      trendType: 'neutral',
      icon: 'ri-checkbox-circle-line', 
      color: 'bg-emerald-50 text-emerald-700',
      trendColor: 'text-slate-500 bg-slate-50'
    },
    { 
      title: 'Customer Ratings', 
      value: '4.9', 
      subValue: '124 reviews',
      trend: '+0.2', 
      trendType: 'up',
      icon: 'ri-group-line', 
      color: 'bg-purple-50 text-purple-700',
      trendColor: 'text-emerald-700 bg-emerald-50'
    }
  ];

  // Quick Actions
  quickActions = [
    { icon: 'ri-user-add-line', label: 'Add Client', description: 'Register new customer', color: 'bg-[#1b75bc]' },
    { icon: 'ri-calendar-line', label: 'Book Appointment', description: 'Schedule service bay', color: 'bg-purple-600' },
    { icon: 'ri-add-line', label: 'Stock Update', description: 'Add inventory items', color: 'bg-amber-600' },
    { icon: 'ri-error-warning-line', label: 'Scan Codes', description: 'OBD-II Diagnostic tool', color: 'bg-red-600' }
  ];

  // Chart Data
  chartData = [
    { name: 'Mon', revenue: 4000, height: '100%', color: 'bg-blue-300' },
    { name: 'Tue', revenue: 3000, height: '75%', color: 'bg-blue-300' },
    { name: 'Wed', revenue: 2000, height: '50%', color: 'bg-blue-300' },
    { name: 'Thu', revenue: 2780, height: '69.5%', color: 'bg-blue-300' },
    { name: 'Fri', revenue: 1890, height: '47.25%', color: 'bg-blue-300' },
    { name: 'Sat', revenue: 2390, height: '59.75%', color: 'bg-blue-300' },
    { name: 'Sun', revenue: 3490, height: '87.25%', color: 'bg-[#1b75bc]' },
  ];

  // Activity Feed
  activities = [
    { time: '2m ago', user: 'Mark (Tech)', action: 'Completed Oil Change', car: 'Honda CR-V', color: 'bg-emerald-500' },
    { time: '15m ago', user: 'System', action: 'New Appointment', car: 'BMW 3 Series', color: 'bg-[#1b75bc]' },
    { time: '45m ago', user: 'Parts', action: 'Low Stock Alert', car: 'Brake Pads (XL)', color: 'bg-red-500' },
    { time: '1h ago', user: 'Reception', action: 'Invoice Generated', car: '#INV-9012', color: 'bg-purple-500' },
    { time: '2h ago', user: 'Sarah (Admin)', action: 'Customer Review', car: '5 Stars ★★★★★', color: 'bg-amber-500' },
  ];

  // Recent Jobs Data
  recentJobs = [
    { id: 'JOB-9021', name: 'John Doe', car: 'Tesla Model 3', plate: 'EV-X23', status: 'In Progress', ready: '2:30 PM', color: 'bg-[#1b75bc]', statusColor: 'text-[#1b75bc]' },
    { id: 'JOB-9020', name: 'Sarah Smith', car: 'Audi A4', plate: 'DE-991', status: 'Pending', ready: '4:00 PM', color: 'bg-slate-300', statusColor: 'text-slate-600' },
    { id: 'JOB-9019', name: 'Mike Johnson', car: 'Toyota Rav4', plate: 'TX-440', status: 'Completed', ready: 'Ready Now', color: 'bg-emerald-500', statusColor: 'text-emerald-700' },
    { id: 'JOB-9018', name: 'Emily Davis', car: 'Honda Civic', plate: 'CA-001', status: 'Delayed', ready: 'Mon 9 AM', color: 'bg-red-500', statusColor: 'text-red-700' }
  ];

  ngOnInit() {
    if (!this.user()) {
      this.authService.me().subscribe();
    }
  }
}
