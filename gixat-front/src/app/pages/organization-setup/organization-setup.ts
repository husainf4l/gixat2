import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LogoComponent } from '../../components/logo/logo.component';

@Component({
  selector: 'app-organization-setup',
  standalone: true,
  imports: [CommonModule, LogoComponent],
  template: `
    <div class="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-6">
      <div class="mb-12">
        <app-logo [height]="40" />
      </div>
      
      <div class="max-w-2xl w-full grid grid-cols-1 md:grid-cols-2 gap-8">
        <!-- Create Organization -->
        <div class="bg-white p-8 rounded-3xl shadow-sm border border-slate-200 hover:border-[#1b75bc] transition-all duration-300 group cursor-pointer">
          <div class="w-14 h-14 bg-blue-50 rounded-2xl flex items-center justify-center mb-6 group-hover:bg-[#1b75bc] transition-colors duration-300">
            <i class="ri-add-line text-2xl text-[#1b75bc] group-hover:text-white"></i>
          </div>
          <h2 class="text-xl font-bold text-slate-900 mb-2">Create Organization</h2>
          <p class="text-slate-500 text-sm leading-relaxed mb-6">
            Start a new workspace for your team. You'll be the administrator of this organization.
          </p>
          <button class="w-full py-3 bg-slate-900 text-white rounded-xl font-medium hover:bg-slate-800 transition-colors">
            Get Started
          </button>
        </div>

        <!-- Join Organization -->
        <div class="bg-white p-8 rounded-3xl shadow-sm border border-slate-200 hover:border-[#1b75bc] transition-all duration-300 group cursor-pointer">
          <div class="w-14 h-14 bg-green-50 rounded-2xl flex items-center justify-center mb-6 group-hover:bg-green-600 transition-colors duration-300">
            <i class="ri-community-line text-2xl text-green-600 group-hover:text-white"></i>
          </div>
          <h2 class="text-xl font-bold text-slate-900 mb-2">Join Organization</h2>
          <p class="text-slate-500 text-sm leading-relaxed mb-6">
            Connect to an existing organization using an invite code or organization ID.
          </p>
          <button class="w-full py-3 border border-slate-200 text-slate-900 rounded-xl font-medium hover:bg-slate-50 transition-colors">
            Connect
          </button>
        </div>
      </div>

      <p class="mt-12 text-slate-400 text-sm">
        Need help? <a href="#" class="text-[#1b75bc] hover:underline">Contact support</a>
      </p>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class OrganizationSetupComponent {
  private router = inject(Router);
}
