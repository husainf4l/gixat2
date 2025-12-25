import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { AiChatAssistantComponent } from '../../components/ai-chat-assistant/ai-chat-assistant.component';
import { LiveWorkshopAssistantComponent } from '../../components/live-workshop-assistant/live-workshop-assistant.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, AiChatAssistantComponent, LiveWorkshopAssistantComponent],
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

  // Live Workshop Assistant
  showLiveAssistant = signal<boolean>(false);

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
    { icon: 'ri-mic-line', label: 'Live Assistant', description: 'Hands-free diagnostics', color: 'bg-[#1b75bc]', action: 'live-assistant' },
    { icon: 'ri-user-add-line', label: 'Add Client', description: 'Register new customer', color: 'bg-purple-600' },
    { icon: 'ri-calendar-line', label: 'Book Appointment', description: 'Schedule service bay', color: 'bg-amber-600' },
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

  handleQuickAction(action: any) {
    if (action.action === 'live-assistant') {
      this.showLiveAssistant.set(true);
    }
    // Add other action handlers here in the future
  }

  getStatusBadgeClass(status: string): string {
    const statusClasses: { [key: string]: string } = {
      'In Progress': 'bg-blue-50 text-blue-700 border-blue-200',
      'Pending': 'bg-slate-100 text-slate-700 border-slate-200',
      'Completed': 'bg-emerald-50 text-emerald-700 border-emerald-200',
      'Delayed': 'bg-red-50 text-red-700 border-red-200'
    };
    return statusClasses[status] || 'bg-slate-100 text-slate-700 border-slate-200';
  }
}
