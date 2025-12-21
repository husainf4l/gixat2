import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/auth/login/login.component';
import { SignupComponent } from './pages/auth/signup/signup.component';
import { CallbackComponent } from './pages/auth/callback/callback.component';
import { DashboardComponent } from './pages/dashboard/dashboard';
import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout';
import { OrganizationSetupComponent } from './pages/organization-setup/organization-setup';
import { authGuard } from './auth.guard';
import { CustomersComponent } from './pages/customers/customers.component';
import { CustomerDetail } from './pages/customers/customer-detail/customer-detail';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/register', component: SignupComponent },
  { path: 'auth/callback', component: CallbackComponent },
  { path: 'organization-setup', component: OrganizationSetupComponent, canActivate: [authGuard] },
  {
    path: 'dashboard',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'customers', component: CustomersComponent },
      { path: 'customers/:id', component: CustomerDetail }
    ]
  }
];
