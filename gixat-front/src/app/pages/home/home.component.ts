import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeroSectionComponent } from "./components/hero-section/hero-section.component";
import { LogoComponent } from '../../components/logo/logo.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, HeroSectionComponent, LogoComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
  scrollToSection(sectionId: string) {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  }

  features = [
    {
      icon: 'ri-calendar-line',
      title: 'Smart Scheduling',
      description: 'Drag-and-drop visual calendar. Manage bays, technicians, and vehicle queues effortlessly. Never double-book again.',
      image: '/hero/generated_images/digital_scheduling_calendar_interface.png',
      iconColor: 'text-[#1b75bc]'
    },
    {
      icon: 'ri-tools-line',
      title: 'Parts Inventory',
      description: 'Real-time tracking with automatic low-stock alerts. Sync with suppliers and manage reorders seamlessly.',
      image: '/hero/generated_images/inventory_management_system_visualization.png',
      iconColor: 'text-orange-500'
    },
    {
      icon: 'ri-team-line',
      title: 'Customer CRM',
      description: 'Complete vehicle history, service reminders, and communication history. Build lasting customer relationships.',
      image: '/hero/generated_images/customer_crm_interface_visualization.png',
      iconColor: 'text-indigo-500'
    },
    {
      icon: 'ri-message-3-line',
      title: 'Customer Communication',
      description: 'Send quotes, invoices, and status updates via SMS and email. Get approvals faster and get paid sooner.',
      image: '/hero/generated_images/modern_auto_repair_shop_interior.png',
      iconColor: 'text-blue-500'
    }
  ];

  benefits = [
    {
      icon: 'ri-time-line',
      iconColor: 'text-orange-500',
      title: 'Save 10+ Hours',
      description: 'Average time saved per week on administrative tasks. Get back to what matters: fixing cars and serving customers.'
    },
    {
      icon: 'ri-shield-check-line',
      iconColor: 'text-green-500',
      title: 'Bank-Level Security',
      description: 'Your customer data is encrypted, backed up daily, and compliant with industry standards. Your data is safe.'
    },
    {
      icon: 'ri-bar-chart-box-line',
      iconColor: 'text-blue-500',
      title: 'Grow Revenue',
      description: 'Our customers see an average 15-25% increase in revenue within the first year through better efficiency and customer retention.'
    }
  ];

  testimonials = [
    {
      quote: 'Gixat cut our scheduling chaos in half. What used to take 30 minutes now takes 5. Our customers love the automatic updates too.',
      author: 'Marcus Rodriguez',
      role: 'Shop Owner, Rodriguez Auto Repair',
      rating: 5
    },
    {
      quote: 'We\'ve been using Gixat for 6 months. The inventory tracking alone has saved us thousands in wasted parts. Highly recommend.',
      author: 'Sarah Chen',
      role: 'Manager, FastTrack Garage',
      rating: 5
    },
    {
      quote: 'Finally, software that\'s actually built for mechanics, not just accountants. The UI is intuitive and the support team is responsive.',
      author: 'James Patterson',
      role: 'Owner, Patterson\'s Automotive',
      rating: 5
    }
  ];

  chartData = [40, 65, 50, 80, 55, 90, 70, 85, 60, 75];
}
