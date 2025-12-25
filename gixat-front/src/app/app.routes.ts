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
import { SessionsComponent } from './pages/sessions/sessions.component';
import { SessionDetailComponent } from './pages/sessions/session-detail/session-detail.component';
import { RequestWidgetComponent } from './pages/sessions/request-widget/request-widget.component';
import { JobCardsComponent } from './pages/job-cards/job-cards.component';
import { JobCardDetailComponent } from './pages/job-cards/job-card-detail/job-card-detail.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { UserDetailComponent } from './pages/profile/user-detail/user-detail.component';
import { VehicleDetailComponent } from './pages/vehicles/vehicle-detail/vehicle-detail';
import { VehiclesComponent } from './pages/vehicles/vehicles.component';
import { AppointmentsComponent } from './pages/appointments/appointments.component';
import { EstimateViewComponent } from './pages/estimates/estimate-view/estimate-view.component';
import { PublicEstimateComponent } from './pages/estimates/public-estimate/public-estimate.component';
import { PrivacyPolicyComponent } from './pages/legal/privacy-policy/privacy-policy.component';
import { TermsOfServiceComponent } from './pages/legal/terms-of-service/terms-of-service.component';
import { DataDeletionComponent } from './pages/legal/data-deletion/data-deletion.component';
import { FeaturesComponent } from './pages/features/features.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'features', component: FeaturesComponent },
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/register', component: SignupComponent },
  { path: 'auth/callback', component: CallbackComponent },
  { path: 'organization-setup', component: OrganizationSetupComponent, canActivate: [authGuard] },
  // Public estimate route (no auth required)
  { path: 'e/:token', component: PublicEstimateComponent },
  // Legal pages (public, no auth required)
  { path: 'privacy-policy', component: PrivacyPolicyComponent },
  { path: 'terms-of-service', component: TermsOfServiceComponent },
  { path: 'data-deletion', component: DataDeletionComponent },
  {
    path: 'dashboard',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'customers', component: CustomersComponent },
      { path: 'customers/:id', component: CustomerDetail },
      { path: 'vehicles', component: VehiclesComponent },
      { path: 'vehicles/:vehicleId', component: VehicleDetailComponent },
      { path: 'appointments', component: AppointmentsComponent },
      { path: 'sessions', component: SessionsComponent },
      { path: 'sessions/:sessionId/request-widget', component: RequestWidgetComponent },
      { path: 'sessions/:id', component: SessionDetailComponent },
      { path: 'job-cards', component: JobCardsComponent },
      { path: 'job-cards/:id', component: JobCardDetailComponent },
      { path: 'estimates/:id', component: EstimateViewComponent },
      { path: 'organization', component: ProfileComponent },
      { path: 'organization/:userId', component: UserDetailComponent }
    ]
  }
];
