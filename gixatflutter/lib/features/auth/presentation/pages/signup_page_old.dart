import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../../core/widgets/gixat_widgets.dart';
import '../bloc/auth_cubit.dart';

class SignUpPage extends StatefulWidget {
  const SignUpPage({Key? key}) : super(key: key);

  @override
  State<SignUpPage> createState() => _SignUpPageState();
}

class _SignUpPageState extends State<SignUpPage> {
  final _formKey = GlobalKey<FormState>();
  final _garageNameController = TextEditingController();
  final _ownerNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _garageNameController.dispose();
    _ownerNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  String? _validateRequired(String? value) {
    if (value == null || value.isEmpty) {
      return 'This field is required';
    }
    return null;
  }

  String? _validateEmail(String? value) {
    if (value == null || value.isEmpty) {
      return 'Email is required';
    }
    if (!RegExp(r'^[^@]+@[^@]+\.[^@]+').hasMatch(value)) {
      return 'Enter a valid email';
    }
    return null;
  }

  String? _validatePassword(String? value) {
    if (value == null || value.isEmpty) {
      return 'Password is required';
    }
    if (value.length < 8) {
      return 'Password must be at least 8 characters';
    }
    if (!RegExp(r'[A-Z]').hasMatch(value)) {
      return 'Must contain uppercase letter';
    }
    if (!RegExp(r'[0-9]').hasMatch(value)) {
      return 'Must contain number';
    }
    return null;
  }

  String? _validateConfirmPassword(String? value) {
    if (value == null || value.isEmpty) {
      return 'Please confirm password';
    }
    if (value != _passwordController.text) {
      return 'Passwords do not match';
    }
    return null;
  }

  void _handleSignUp(BuildContext context) {
    if (_formKey.currentState!.validate()) {
      context.read<AuthCubit>().register(
            garageName: _garageNameController.text.trim(),
            ownerName: _ownerNameController.text.trim(),
            email: _emailController.text.trim(),
            password: _passwordController.text,
          );
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: AppTheme.background,
        appBar: AppBar(
          leading: GestureDetector(
            onTap: () => context.pop(),
            child: const Icon(Icons.arrow_back_ios_new, size: 20),
          ),
          elevation: 0,
          backgroundColor: AppTheme.background,
          title: const SizedBox.shrink(),
        ),
        body: BlocConsumer<AuthCubit, AuthState>(
          listener: (context, state) {
            if (state is AuthError) {
              showGixatSnackbar(
                context,
                message: state.message,
                isError: true,
              );
            } else if (state is AuthAuthenticated) {
              // Navigate to dashboard
              context.go('/dashboard');
            }
          },
          builder: (context, state) {
            final isLoading = state is AuthLoading;

            return SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.all(AppTheme.spacing24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Header
                    Text(
                      'Create your account',
                      style: Theme.of(context).textTheme.displaySmall?.copyWith(
                            fontWeight: FontWeight.w700,
                          ),
                    ),
                    const SizedBox(height: AppTheme.spacing8),
                    Text(
                      'Build your garage management hub',
                      style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                            color: AppTheme.textLight,
                          ),
                    ),
                    const SizedBox(height: AppTheme.spacing32),

                    // Form
                    Form(
                      key: _formKey,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Garage Name Field
                          GixatTextField(
                            label: 'Garage Name',
                            hintText: 'e.g. John\'s Auto Repair',
                            controller: _garageNameController,
                            validator: _validateRequired,
                            enabled: !isLoading,
                            prefixIcon: const Icon(
                              Icons.business_outlined,
                              color: AppTheme.textLight,
                              size: 20,
                            ),
                          ),
                          const SizedBox(height: AppTheme.spacing20),

                          // Owner Name Field
                          GixatTextField(
                            label: 'Owner Name',
                            hintText: 'Your full name',
                            controller: _ownerNameController,
                            validator: _validateRequired,
                            enabled: !isLoading,
                            prefixIcon: const Icon(
                              Icons.person_outline,
                              color: AppTheme.textLight,
                              size: 20,
                            ),
                          ),
                          const SizedBox(height: AppTheme.spacing20),

                          // Email Field
                          GixatTextField(
                            label: 'Email',
                            hintText: 'you@example.com',
                            controller: _emailController,
                            validator: _validateEmail,
                            keyboardType: TextInputType.emailAddress,
                            enabled: !isLoading,
                            prefixIcon: const Icon(
                              Icons.mail_outline,
                              color: AppTheme.textLight,
                              size: 20,
                            ),
                          ),
                          const SizedBox(height: AppTheme.spacing20),

                          // Password Field
                          GixatTextField(
                            label: 'Password',
                            hintText: 'At least 8 characters',
                            controller: _passwordController,
                            validator: _validatePassword,
                            obscureText: true,
                            enabled: !isLoading,
                            prefixIcon: const Icon(
                              Icons.lock_outline,
                              color: AppTheme.textLight,
                              size: 20,
                            ),
                          ),
                          const SizedBox(height: AppTheme.spacing12),

                          // Password Requirements Helper
                          Container(
                            padding: const EdgeInsets.all(AppTheme.spacing12),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius:
                                  BorderRadius.circular(AppTheme.radiusMedium),
                              border: Border.all(
                                color: AppTheme.border,
                                width: 1,
                              ),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                _PasswordRequirement(
                                  text: 'At least 8 characters',
                                  isMet: _passwordController.text.length >= 8,
                                ),
                                const SizedBox(height: AppTheme.spacing8),
                                _PasswordRequirement(
                                  text: 'One uppercase letter',
                                  isMet: RegExp(r'[A-Z]')
                                      .hasMatch(_passwordController.text),
                                ),
                                const SizedBox(height: AppTheme.spacing8),
                                _PasswordRequirement(
                                  text: 'One number',
                                  isMet: RegExp(r'[0-9]')
                                      .hasMatch(_passwordController.text),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: AppTheme.spacing20),

                          // Confirm Password Field
                          GixatTextField(
                            label: 'Confirm Password',
                            hintText: 'Re-enter your password',
                            controller: _confirmPasswordController,
                            validator: _validateConfirmPassword,
                            obscureText: true,
                            enabled: !isLoading,
                            prefixIcon: const Icon(
                              Icons.lock_outline,
                              color: AppTheme.textLight,
                              size: 20,
                            ),
                          ),
                          const SizedBox(height: AppTheme.spacing32),

                          // Sign Up Button
                          SizedBox(
                            width: double.infinity,
                            height: 56,
                            child: GixatButton(
                              label: 'Create Account',
                              onPressed: () => _handleSignUp(context),
                              isLoading: isLoading,
                              isEnabled: !isLoading,
                            ),
                          ),
                          const SizedBox(height: AppTheme.spacing20),

                          // Login Link
                          Center(
                            child: RichText(
                              text: TextSpan(
                                style: Theme.of(context)
                                    .textTheme
                                    .bodyMedium
                                    ?.copyWith(
                                      color: AppTheme.textLight,
                                    ),
                                children: [
                                  const TextSpan(
                                      text: 'Already have an account? '),
                                  WidgetSpan(
                                    child: GestureDetector(
                                      onTap: isLoading
                                          ? null
                                          : () {
                                              context.pop();
                                            },
                                      child: Text(
                                        'Login',
                                        style: Theme.of(context)
                                            .textTheme
                                            .bodyMedium
                                            ?.copyWith(
                                              color: AppTheme.primary,
                                              fontWeight: FontWeight.w600,
                                            ),
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            );
          },
        ),
      );
}

/// Helper widget for password requirements
class _PasswordRequirement extends StatelessWidget {
  const _PasswordRequirement({
    required this.text,
    required this.isMet,
  });
  final String text;
  final bool isMet;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Container(
            width: 20,
            height: 20,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: isMet ? AppTheme.success : AppTheme.border,
            ),
            child: Center(
              child: Icon(
                isMet ? Icons.check : Icons.close,
                size: 12,
                color: isMet ? Colors.white : AppTheme.textLight,
              ),
            ),
          ),
          const SizedBox(width: AppTheme.spacing8),
          Text(
            text,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: isMet ? AppTheme.success : AppTheme.textLight,
                ),
          ),
        ],
      );
}
