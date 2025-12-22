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
import '../features/auth/presentation/pages/garage_setup_page.dart';
import '../features/auth/presentation/pages/login_page.dart';
import '../features/auth/presentation/pages/signup_page.dart';
import '../features/auth/presentation/pages/splash_page.dart';
import '../features/clients/presentation/pages/clients_page.dart';
import '../features/clients/presentation/widgets/create_customer_form.dart';
import '../features/clients/presentation/pages/create_car_page.dart';
import '../features/clients/data/repositories/clients_repository.dart';
import '../features/clients/presentation/bloc/clients_cubit.dart';
import '../features/clients/presentation/bloc/create_car_cubit.dart';
import '../features/inventory/presentation/pages/inventory_page.dart';
import '../features/invoices/presentation/pages/invoices_page.dart';
import '../features/jobcards/presentation/pages/jobcards_page.dart';
import '../features/jobcards/presentation/pages/create_jobcard_page.dart';
import '../features/jobcards/presentation/bloc/create_jobcard_cubit.dart';
import '../features/navigation/app_layout.dart';
import '../features/sessions/presentation/pages/sessions_page.dart';
import '../features/sessions/presentation/pages/create_session_page.dart';
import '../features/sessions/presentation/bloc/create_session_cubit.dart';
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
          path: '/garage-setup',
          name: 'garage-setup',
          builder: (context, state) {
            final extra = state.extra as Map<String, dynamic>?;
            return GarageSetupPage(
              email: extra?['email'] ?? '',
              password: extra?['password'] ?? '',
              fullName: extra?['fullName'] ?? '',
            );
          },
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
              routes: [
                GoRoute(
                  path: 'create',
                  name: 'create-session',
                  builder: (context, state) {
                    final client = GraphQLProvider.of(context).value;
                    return BlocProvider(
                      create: (context) => CreateSessionCubit(client: client),
                      child: const CreateSessionPage(),
                    );
                  },
                ),
              ],
            ),
            GoRoute(
              path: '/clients',
              name: 'clients',
              pageBuilder: (context, state) => const NoTransitionPage(
                child: ClientsPage(),
              ),
              routes: [
                GoRoute(
                  path: 'create',
                  name: 'create-client',
                  builder: (context, state) {
                    return BlocProvider(
                      create: (context) => ClientsCubit(
                        clientsRepository: ClientsRepository(),
                      ),
                      child: const CreateCustomerForm(),
                    );
                  },
                ),
                GoRoute(
                  path: 'add-car',
                  name: 'add-car',
                  builder: (context, state) {
                    final customerId = state.uri.queryParameters['customerId'] ?? '';
                    final customerName = state.uri.queryParameters['customerName'] ?? '';
                    final client = GraphQLProvider.of(context).value;
                    return BlocProvider(
                      create: (context) => CreateCarCubit(client: client),
                      child: CreateCarPage(
                        customerId: customerId,
                        customerName: customerName,
                      ),
                    );
                  },
                ),
              ],
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
              routes: [
                GoRoute(
                  path: 'create',
                  name: 'create-job-card',
                  builder: (context, state) {
                    final client = GraphQLProvider.of(context).value;
                    return BlocProvider(
                      create: (context) => CreateJobCardCubit(client: client),
                      child: const CreateJobCardPage(),
                    );
                  },
                ),
              ],
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
