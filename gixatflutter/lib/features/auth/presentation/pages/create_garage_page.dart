import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/widgets/auth_text_field.dart';
import '../../../../core/widgets/gradient_background.dart';
import '../../../../core/widgets/gradient_button.dart';
import '../bloc/auth_cubit.dart';

class CreateGaragePage extends StatefulWidget {
  const CreateGaragePage({super.key});

  @override
  State<CreateGaragePage> createState() => _CreateGaragePageState();
}

class _CreateGaragePageState extends State<CreateGaragePage> {
  final _nameController = TextEditingController();
  final _countryController =
      TextEditingController(text: 'United Arab Emirates');
  final _cityController = TextEditingController();
  final _streetController = TextEditingController();
  final _phoneCodeController = TextEditingController(text: '+971');

  @override
  void dispose() {
    _nameController.dispose();
    _countryController.dispose();
    _cityController.dispose();
    _streetController.dispose();
    _phoneCodeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => BlocListener<AuthCubit, AuthState>(
      listener: (context, state) {
        if (state is AuthAuthenticated) {
          context.go('/sessions');
        } else if (state is AuthError) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(state.message),
              backgroundColor: Colors.redAccent,
            ),
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
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SizedBox(height: 20),
                  Text(
                    'Create Garage',
                    style: Theme.of(context).textTheme.headlineLarge?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Enter your business details to get started.',
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                          color: Colors.white.withValues(alpha: 0.7),
                        ),
                  ),
                  const SizedBox(height: 40),
                  AuthTextField(
                    controller: _nameController,
                    hint: 'Garage Name',
                    icon: CupertinoIcons.house,
                  ),
                  const SizedBox(height: 16),
                  AuthTextField(
                    controller: _countryController,
                    hint: 'Country',
                    icon: CupertinoIcons.globe,
                  ),
                  const SizedBox(height: 16),
                  AuthTextField(
                    controller: _cityController,
                    hint: 'City',
                    icon: CupertinoIcons.location,
                  ),
                  const SizedBox(height: 16),
                  AuthTextField(
                    controller: _streetController,
                    hint: 'Street Address',
                    icon: CupertinoIcons.map_pin,
                  ),
                  const SizedBox(height: 16),
                  AuthTextField(
                    controller: _phoneCodeController,
                    hint: 'Phone Country Code',
                    icon: CupertinoIcons.phone,
                    keyboardType: TextInputType.phone,
                  ),
                  const SizedBox(height: 40),
                  BlocBuilder<AuthCubit, AuthState>(
                    builder: (context, state) => GradientButton(
                        label: 'Create & Continue',
                        isLoading: state is AuthLoading,
                        onPressed: () {
                          if (_nameController.text.isEmpty) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                  content: Text('Please enter a garage name')),
                            );
                            return;
                          }
                          if (_cityController.text.isEmpty) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                  content: Text('Please enter a city')),
                            );
                            return;
                          }

                          context.read<AuthCubit>().createOrganization(
                                name: _nameController.text,
                                country: _countryController.text,
                                city: _cityController.text,
                                street: _streetController.text,
                                phoneCountryCode: _phoneCodeController.text,
                              );
                        },
                      ),
                  ),
                  const SizedBox(height: 40),
                ],
              ),
            ),
          ),
        ),
      ),
    );
}
