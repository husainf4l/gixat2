import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import '../bloc/auth_cubit.dart';
import '../widgets/gixat_logo_splash.dart';

class SplashPage extends StatefulWidget {
  const SplashPage({Key? key}) : super(key: key);

  @override
  State<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends State<SplashPage> {
  @override
  void initState() {
    super.initState();
    // Check auth when splash loads
    Future.delayed(const Duration(milliseconds: 500), () {
      if (mounted) {
        context.read<AuthCubit>().checkAuth();
      }
    });
  }

  @override
  Widget build(BuildContext context) => BlocListener<AuthCubit, AuthState>(
        listener: (context, state) {
          if (state is AuthAuthenticated) {
            context.go('/sessions');
          } else if (state is AuthNeedsGarage) {
            context.go('/garage-selection');
          } else if (state is AuthUnauthenticated) {
            context.go('/login');
          }
        },
        child: const Scaffold(
          backgroundColor: Colors.white,
          body: Center(
            child: GixatLogoSplash(logoText: 'GIXAT'),
          ),
        ),
      );
}
