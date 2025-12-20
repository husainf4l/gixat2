# Gixat Dashboard Implementation Guide

## Current Status
✅ GraphQL client configuration created
✅ Authentication queries defined (login, register, verify, me)
✅ Dashboard queries defined (stats, appointments, job cards, alerts)

## Next Steps to Complete

### 1. Update Authentication (HIGH PRIORITY)

**File: lib/features/auth/presentation/bloc/auth_cubit.dart**

Replace Dio HTTP calls with GraphQL mutations:
- Use `loginMutation` from `auth_queries.dart`
- Call GraphQL client instead of Dio
- Store token from `tokenAuth.token`
- Handle GraphQL errors

### 2. Create Navigation Structure

**File: lib/features/navigation/sidebar.dart**
- Main section: Dashboard, Sessions, Clients, Appointments, Job Cards
- Business section: Invoices, Inventory
- Settings section: Settings
- Icons: Icons.dashboard, Icons.access_time, Icons.people, Icons.calendar_today, Icons.build, Icons.receipt, Icons.inventory, Icons.settings
- Active state highlighting
- Mobile responsive

### 3. Create Dashboard Page

**File: lib/features/dashboard/presentation/pages/dashboard_page.dart**

Sections:
1. Page header (title, notifications icon, user avatar)
2. Quick stats (4 stat cards)
3. Today's schedule (scrollable list)
4. Active job cards (table-style)
5. Alerts (conditional)

### 4. Create Dashboard Widgets

**Files in lib/features/dashboard/presentation/widgets/**:
- `stat_tile.dart` - Small card with number + label
- `today_schedule_item.dart` - Appointment row with time/client/vehicle/status
- `job_card_row.dart` - Job card row with all details
- `alert_card.dart` - Alert with color coding and CTA

### 5. Create Dashboard State Management

**File: lib/features/dashboard/presentation/bloc/dashboard_cubit.dart**

States:
- DashboardInitial
- DashboardLoading
- DashboardLoaded (with all data)
- DashboardError

Methods:
- `loadDashboard()` - Fetches all dashboard data
- `refreshDashboard()` - Refreshes data

### 6. Update Router

**File: lib/router/app_router.dart**

Add dashboard route with sidebar:
```dart
GoRoute(
  path: '/dashboard',
  builder: (context, state) => DashboardWithSidebar(),
)
```

### 7. Connect to GraphQL Endpoint

**Test with introspection query first:**
```graphql
query IntrospectionQuery {
  __schema {
    types {
      name
      fields {
        name
        type {
          name
        }
      }
    }
  }
}
```

This will show the actual schema from https://gixat.com/graphql/

## GraphQL Schema Assumptions

Based on common patterns, assuming:
- `tokenAuth(email, password)` for login
- `createUser(email, password, firstName, lastName)` for registration  
- `dashboardStats`, `todayAppointments`, `activeJobCards`, `alerts` for dashboard

**You must verify with actual schema introspection!**

## Files Created So Far

1. `/lib/core/graphql/graphql_client.dart` - GraphQL client configuration
2. `/lib/core/graphql/auth_queries.dart` - Login/register/verify queries
3. `/lib/core/graphql/dashboard_queries.dart` - Dashboard data queries

## Recommended Implementation Order

1. ✅ GraphQL setup (DONE)
2. ⏳ Update AuthCubit with real GraphQL (NEXT)
3. Create sidebar navigation
4. Create dashboard page layout
5. Create dashboard widgets
6. Create dashboard cubit
7. Connect everything and test

## Important Notes

- The GraphQL schema at https://gixat.com/graphql/ may differ from assumptions
- Run introspection query first to get actual field names
- Adjust query/mutation names based on actual schema
- Test authentication flow thoroughly before building dashboard
- Mobile-first responsive design
- Handle loading/empty/error states everywhere

## Production Checklist

- [ ] GraphQL error handling
- [ ] Token refresh logic
- [ ] Role-based access control
- [ ] Offline support (cache)
- [ ] Loading skeletons
- [ ] Empty states with helpful messages
- [ ] Error messages that users understand
- [ ] Mobile responsive breakpoints
- [ ] Accessibility (screen readers, contrast)
- [ ] Performance (lazy loading, pagination)
