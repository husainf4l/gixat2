import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/widgets/auth_text_field.dart';
import '../../../../core/widgets/gradient_background.dart';
import '../../../../core/widgets/gradient_button.dart';
import '../bloc/auth_cubit.dart';

class ConnectGaragePage extends StatefulWidget {
  const ConnectGaragePage({super.key});

  @override
  State<ConnectGaragePage> createState() => _ConnectGaragePageState();
}

class _ConnectGaragePageState extends State<ConnectGaragePage> {
  final _codeController = TextEditingController();

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => BlocListener<AuthCubit, AuthState>(
      listener: (context, state) {
        if (state is AuthAuthenticated) {
          context.go('/sessions');
        } else if (state is AuthError) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(state.message)),
          );
        }
      },
      child: Scaffold(
        extendBodyBehindAppBar: true,
        appBar: AppBar(
          backgroundColor: Colors.transparent,
          elevation: 0,
          leading: IconButton(
            icon: const Icon(CupertinoIcons.back, color: Colors.white),
            onPressed: () => context.pop(),
          ),
        ),
        body: GradientBackground(
          child: SafeArea(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SizedBox(height: 20),
                  Text(
                    'Connect Garage',
                    style: Theme.of(context).textTheme.headlineLarge?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Enter the invitation code or Garage ID provided by '
                    'your administrator.',
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                          color: Colors.white.withValues(alpha: 0.7),
                        ),
                  ),
                  const SizedBox(height: 40),
                  AuthTextField(
                    controller: _codeController,
                    hint: 'Invitation Code / Garage ID',
                    icon: CupertinoIcons.number,
                  ),
                  const SizedBox(height: 40),
                  GradientButton(
                    label: 'Connect',
                    onPressed: () {
                      if (_codeController.text.isNotEmpty) {
                        context
                            .read<AuthCubit>()
                            .selectGarage(_codeController.text);
                      } else {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Please enter a code')),
                        );
                      }
                    },
                  ),
                  const Spacer(),
                  Center(
                    child: Text(
                      'Don\'t have a code? Contact your garage owner.',
                      style: TextStyle(
                        color: Colors.white.withValues(alpha: 0.5),
                        fontSize: 14,
                      ),
                    ),
                  ),
                  const SizedBox(height: 20),
                ],
              ),
            ),
          ),
        ),
      ),
    );
}
