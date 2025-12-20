import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import '../features/auth/presentation/bloc/auth_cubit.dart';
import '../features/auth/presentation/pages/splash_page.dart';
import '../features/auth/presentation/pages/login_page.dart';
import '../features/auth/presentation/pages/signup_page.dart';
import '../features/dashboard/presentation/pages/dashboard_page.dart';
import '../features/sessions/presentation/pages/sessions_page.dart';
import '../features/clients/presentation/pages/clients_page.dart';
import '../features/appointments/presentation/pages/appointments_page.dart';
import '../features/jobcards/presentation/pages/jobcards_page.dart';
import '../features/invoices/presentation/pages/invoices_page.dart';
import '../features/inventory/presentation/pages/inventory_page.dart';
import '../features/navigation/app_layout.dart';

GoRouter createAppRouter(AuthCubit authCubit) {
  return GoRouter(
    initialLocation: '/splash',
    refreshListenable: GoRouterRefreshStream(authCubit.stream),
    redirect: (context, state) {
      final authState = context.read<AuthCubit>().state;
      final location = state.matchedLocation;

      // Always allow splash screen to show - never redirect from it
      if (location == '/splash') {
        return null;
      }

      // If user is authenticated, redirect to dashboard
      if (authState is AuthAuthenticated) {
        if (location == '/login' || location == '/signup') {
          return '/dashboard';
        }
      }

      // If user is unauthenticated and not on auth pages, redirect to login
      if (authState is AuthUnauthenticated) {
        if (location != '/login' && location != '/signup') {
          return '/login';
        }
      }

      // Allow all other states
      return null;
    },
  routes: [
    GoRoute(
      path: '/splash',
      name: 'splash',
      builder: (context, state) => const SplashPage(),
    ),
    GoRoute(
      path: '/login',
      name: 'login',
      builder: (context, state) => const LoginPage(),
    ),
    GoRoute(
      path: '/signup',
      name: 'signup',
      builder: (context, state) => const SignUpPage(),
    ),
    GoRoute(
      path: '/dashboard',
      name: 'dashboard',
      builder: (context, state) => const AppLayout(
        currentPath: '/dashboard',
        child: DashboardPage(),
      ),
    ),
    GoRoute(
      path: '/sessions',
      name: 'sessions',
      builder: (context, state) => const AppLayout(
        currentPath: '/sessions',
        child: SessionsPage(),
      ),
    ),
    GoRoute(
      path: '/clients',
      name: 'clients',
      builder: (context, state) => const AppLayout(
        currentPath: '/clients',
        child: ClientsPage(),
      ),
    ),
    GoRoute(
      path: '/appointments',
      name: 'appointments',
      builder: (context, state) => const AppLayout(
        currentPath: '/appointments',
        child: AppointmentsPage(),
      ),
    ),
    GoRoute(
      path: '/job-cards',
      name: 'job-cards',
      builder: (context, state) => const AppLayout(
        currentPath: '/job-cards',
        child: JobCardsPage(),
      ),
    ),
    GoRoute(
      path: '/invoices',
      name: 'invoices',
      builder: (context, state) => const AppLayout(
        currentPath: '/invoices',
        child: InvoicesPage(),
      ),
    ),
    GoRoute(
      path: '/inventory',
      name: 'inventory',
      builder: (context, state) => const AppLayout(
        currentPath: '/inventory',
        child: InventoryPage(),
      ),
    ),
  ],
  );
}

// Helper class to convert Stream to Listenable for GoRouter
class GoRouterRefreshStream extends ChangeNotifier {
  GoRouterRefreshStream(Stream<dynamic> stream) {
    notifyListeners();
    _subscription = stream.asBroadcastStream().listen(
          (_) => notifyListeners(),
        );
  }

  late final StreamSubscription<dynamic> _subscription;

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }
}
