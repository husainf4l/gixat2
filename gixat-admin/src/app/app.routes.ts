import { Routes } from '@angular/router';
import { Companies } from './companies/companies';
import { Users } from './users/users';
import { SystemLogs } from './system-logs/system-logs';
import { HealthCheckComponent } from './health-check/health-check';
import { Dashboard } from './dashboard/dashboard';
import { LoginComponent } from './login/login.component';
import { AuthGuard } from './auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'dashboard', component: Dashboard, canActivate: [AuthGuard] },
  { path: 'companies', component: Companies, canActivate: [AuthGuard] },
  { path: 'users', component: Users, canActivate: [AuthGuard] },
  { path: 'system-logs', component: SystemLogs, canActivate: [AuthGuard] },
  { path: 'health-check', component: HealthCheckComponent, canActivate: [AuthGuard] },
];
