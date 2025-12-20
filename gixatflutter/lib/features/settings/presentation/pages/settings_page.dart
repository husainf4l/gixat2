import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../auth/presentation/bloc/auth_cubit.dart';

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Text('Settings Page'),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: () {
                  context.read<AuthCubit>().logout();
                },
                child: const Text('Sign Out'),
              ),
            ],
          ),
        ),
      );
}
