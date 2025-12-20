import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:graphql_flutter/graphql_flutter.dart';

import '../core/graphql/graphql_client.dart';

import '../features/appointments/presentation/pages/appointments_page.dart';
import '../features/auth/presentation/bloc/auth_cubit.dart';
import '../features/auth/presentation/pages/connect_garage_page.dart';
import '../features/auth/presentation/pages/create_garage_page.dart';
import '../features/auth/presentation/pages/garage_selection_page.dart';
import '../features/auth/presentation/pages/login_page.dart';
import '../features/auth/presentation/pages/signup_page.dart';
import '../features/auth/presentation/pages/splash_page.dart';
import '../features/clients/presentation/pages/clients_page.dart';
import '../features/inventory/presentation/pages/inventory_page.dart';
import '../features/invoices/presentation/pages/invoices_page.dart';
import '../features/jobcards/presentation/pages/jobcards_page.dart';
import '../features/navigation/app_layout.dart';
import '../features/sessions/presentation/pages/sessions_page.dart';
import '../features/settings/presentation/pages/settings_page.dart';

GoRouter createAppRouter(AuthCubit authCubit) => GoRouter(
      initialLocation: '/splash',
      refreshListenable: GoRouterRefreshStream(authCubit.stream),
      redirect: (context, state) {
        final authState = authCubit.state;
        final location = state.matchedLocation;

        // If still in initial or loading state, stay on splash
        if (authState is AuthInitial || authState is AuthLoading) {
          return location == '/splash' ? null : '/splash';
        }

        // If user is unauthenticated or there is an error on splash
        if (authState is AuthUnauthenticated || (authState is AuthError && location == '/splash')) {
          final isAuthPage = location == '/login' || location == '/signup';
          if (!isAuthPage) {
            return '/login';
          }
          return null;
        }

        // If user needs a garage
        if (authState is AuthNeedsGarage) {
          final isGaragePage = location == '/garage-selection' || 
                              location == '/create-garage' || 
                              location == '/connect-garage';
          if (!isGaragePage) {
            return '/garage-selection';
          }
          return null;
        }

        // If user is authenticated
        if (authState is AuthAuthenticated) {
          final isAuthPage = location == '/login' || 
                            location == '/signup' || 
                            location == '/splash' ||
                            location == '/garage-selection' ||
                            location == '/create-garage' ||
                            location == '/connect-garage';
          if (isAuthPage) {
            return '/sessions';
          }
          return null;
        }

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
          path: '/garage-selection',
          name: 'garage-selection',
          builder: (context, state) => const GarageSelectionPage(),
        ),
        GoRoute(
          path: '/create-garage',
          name: 'create-garage',
          builder: (context, state) => const CreateGaragePage(),
        ),
        GoRoute(
          path: '/connect-garage',
          name: 'connect-garage',
          builder: (context, state) => const ConnectGaragePage(),
        ),
        ShellRoute(
          builder: (context, state, child) {
            final authCubit = context.read<AuthCubit>();
            
            // Use BlocBuilder to rebuild when auth state changes
            return BlocBuilder<AuthCubit, AuthState>(
              builder: (context, authState) =>
                  FutureBuilder<String?>(
                  future: authCubit.getToken(),
                  builder: (context, snapshot) {
                    // Show loading while fetching token
                    if (snapshot.connectionState != ConnectionState.done) {
                      return const Scaffold(
                        body: Center(child: CircularProgressIndicator()),
                      );
                    }
                    
                    final token = snapshot.data;
                    
                    return GraphQLProvider(
                      client: GraphQLConfig.clientFor(token: token),
                      child: AppLayout(
                        currentPath: state.matchedLocation,
                        child: child,
                      ),
                    );
                  },
                ),
            );
          },
          routes: [
            GoRoute(
              path: '/sessions',
              name: 'sessions',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: SessionsPage(),
              ),
            ),
            GoRoute(
              path: '/clients',
              name: 'clients',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: ClientsPage(),
              ),
            ),
            GoRoute(
              path: '/appointments',
              name: 'appointments',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: AppointmentsPage(),
              ),
            ),
            GoRoute(
              path: '/job-cards',
              name: 'job-cards',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: JobCardsPage(),
              ),
            ),
            GoRoute(
              path: '/invoices',
              name: 'invoices',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: InvoicesPage(),
              ),
            ),
            GoRoute(
              path: '/inventory',
              name: 'inventory',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: InventoryPage(),
              ),
            ),
            GoRoute(
              path: '/settings',
              name: 'settings',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: SettingsPage(),
              ),
            ),
          ],
        ),
      ],
    );

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
