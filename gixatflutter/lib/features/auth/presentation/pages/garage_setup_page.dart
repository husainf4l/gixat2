import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../bloc/auth_cubit.dart';
import '../widgets/auth_background.dart';
import '../widgets/auth_card.dart';
import '../widgets/auth_input.dart';
import '../widgets/gradient_primary_button.dart';

class GarageSetupPage extends StatefulWidget {
  const GarageSetupPage({
    super.key,
    required this.email,
    required this.password,
    required this.fullName,
  });

  final String email;
  final String password;
  final String fullName;

  @override
  State<GarageSetupPage> createState() => _GarageSetupPageState();
}

class _GarageSetupPageState extends State<GarageSetupPage> {
  final _formKey = GlobalKey<FormState>();
  final _garageNameController = TextEditingController();
  final _countryController = TextEditingController(text: 'UAE');
  final _cityController = TextEditingController();
  final _streetController = TextEditingController();
  final _phoneCodeController = TextEditingController(text: '+971');

  @override
  void dispose() {
    _garageNameController.dispose();
    _countryController.dispose();
    _cityController.dispose();
    _streetController.dispose();
    _phoneCodeController.dispose();
    super.dispose();
  }

  void _handleSetup() {
    if (_formKey.currentState?.validate() ?? false) {
      context.read<AuthCubit>().createOrganization(
            name: _garageNameController.text,
            country: _countryController.text,
            city: _cityController.text,
            street: _streetController.text,
            phoneCountryCode: _phoneCodeController.text,
            email: widget.email,
            password: widget.password,
            fullName: widget.fullName,
          );
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: Colors.white,
        body: AuthBackground(
          child: BlocConsumer<AuthCubit, AuthState>(
            listener: (context, state) {
              if (state is AuthAuthenticated) {
                context.go('/sessions');
              } else if (state is AuthError) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(state.message),
                    backgroundColor: const Color(0xFFE74C3C),
                    behavior: SnackBarBehavior.floating,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                );
              }
            },
            builder: (context, state) {
              final isLoading = state is AuthLoading;

              return SafeArea(
                child: Center(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(vertical: 40),
                    child: AuthCard(
                      child: Form(
                        key: _formKey,
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            // Logo
                            Center(
                              child: SizedBox(
                                width: 150,
                                height: 150,
                                child: Image.asset(
                                  'assets/images/gixat-logo.png',
                                  fit: BoxFit.contain,
                                ),
                              ),
                            ),
                            const SizedBox(height: 24),
                            const Text(
                              'Set up your Garage',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w400,
                                color: Color(0xFF7F8C8D),
                              ),
                            ),
                            const SizedBox(height: 40),

                            // Garage Name Field
                            AuthInputField(
                              hint: 'Garage Name',
                              icon: Icons.garage_outlined,
                              controller: _garageNameController,
                              validator: (value) {
                                if (value == null || value.isEmpty) {
                                  return 'Please enter garage name';
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 16),

                            // Country Field
                            AuthInputField(
                              hint: 'Country',
                              icon: Icons.flag_outlined,
                              controller: _countryController,
                              validator: (value) {
                                if (value == null || value.isEmpty) {
                                  return 'Please enter country';
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 16),

                            // City Field
                            AuthInputField(
                              hint: 'City',
                              icon: Icons.location_city_outlined,
                              controller: _cityController,
                              validator: (value) {
                                if (value == null || value.isEmpty) {
                                  return 'Please enter city';
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 16),

                            // Street Address Field
                            AuthInputField(
                              hint: 'Street Address',
                              icon: Icons.location_on_outlined,
                              controller: _streetController,
                              validator: (value) {
                                if (value == null || value.isEmpty) {
                                  return 'Please enter street address';
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 16),

                            // Phone Country Code Field
                            AuthInputField(
                              hint: 'Phone Country Code',
                              icon: Icons.phone_outlined,
                              controller: _phoneCodeController,
                              keyboardType: TextInputType.phone,
                              validator: (value) {
                                if (value == null || value.isEmpty) {
                                  return 'Please enter phone country code';
                                }
                                return null;
                              },
                            ),
                            const SizedBox(height: 32),

                            // Complete Setup Button
                            GradientPrimaryButton(
                              label: 'Complete Setup',
                              onPressed: _handleSetup,
                              isLoading: isLoading,
                              height: 56,
                            ),
                            const SizedBox(height: 24),

                            // Back Link
                            Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                GestureDetector(
                                  onTap: () => context.go('/signup'),
                                  child: const Text(
                                    'Back to Sign Up',
                                    style: TextStyle(
                                      fontSize: 14,
                                      fontWeight: FontWeight.w600,
                                      color: Color(0xFF2193B0),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              );
            },
          ),
        ),
      );
}
