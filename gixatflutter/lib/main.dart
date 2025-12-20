import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import 'core/storage/secure_storage_service.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/auth/presentation/bloc/auth_cubit.dart';
import 'router/app_router.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Performance optimizations
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);

  // Initialize GraphQL
  await initHiveForFlutter();

  // Initialize services
  final storage = SecureStorageService();
  final authRepository = AuthRepository(storage: storage);

  runApp(
    GixatApp(authRepository: authRepository),
  );
}

class GixatApp extends StatelessWidget {
  const GixatApp({
    required this.authRepository,
    Key? key,
  }) : super(key: key);
  final AuthRepository authRepository;

  @override
  Widget build(BuildContext context) => BlocProvider(
        create: (context) => AuthCubit(authRepository: authRepository),
        child: Builder(
          builder: (context) {
            final authCubit = context.read<AuthCubit>();
            final router = createAppRouter(authCubit);

            return MaterialApp.router(
              title: 'Gixat',
              theme: AppTheme.lightTheme,
              routerDelegate: router.routerDelegate,
              routeInformationParser: router.routeInformationParser,
              routeInformationProvider: router.routeInformationProvider,
              backButtonDispatcher: router.backButtonDispatcher,
              debugShowCheckedModeBanner: false,
              // Performance optimizations
              builder: (context, child) => MediaQuery(
                data: MediaQuery.of(context).copyWith(
                  textScaler: TextScaler.noScaling,
                ),
                child: child!,
              ),
            );
          },
        ),
      );
}
