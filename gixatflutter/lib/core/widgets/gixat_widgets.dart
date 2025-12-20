import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

// Custom Text Field
class GixatTextField extends StatefulWidget {
  const GixatTextField({
    required this.label,
    Key? key,
    this.hintText,
    this.controller,
    this.validator,
    this.keyboardType = TextInputType.text,
    this.obscureText = false,
    this.prefixIcon,
    this.suffixIcon,
    this.onChanged,
    this.maxLines = 1,
    this.minLines = 1,
    this.enabled = true,
  }) : super(key: key);
  final String label;
  final String? hintText;
  final TextEditingController? controller;
  final String? Function(String?)? validator;
  final TextInputType keyboardType;
  final bool obscureText;
  final Widget? prefixIcon;
  final Widget? suffixIcon;
  final void Function(String)? onChanged;
  final int maxLines;
  final int minLines;
  final bool enabled;

  @override
  State<GixatTextField> createState() => _GixatTextFieldState();
}

class _GixatTextFieldState extends State<GixatTextField> {
  late bool _obscure;

  @override
  void initState() {
    super.initState();
    _obscure = widget.obscureText;
  }

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            widget.label,
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: AppTheme.spacing8),
          TextFormField(
            controller: widget.controller,
            obscureText: _obscure,
            keyboardType: widget.keyboardType,
            validator: widget.validator,
            onChanged: widget.onChanged,
            maxLines: _obscure ? 1 : widget.maxLines,
            minLines: widget.minLines,
            enabled: widget.enabled,
            decoration: InputDecoration(
              hintText: widget.hintText,
              prefixIcon: widget.prefixIcon,
              suffixIcon: widget.obscureText
                  ? GestureDetector(
                      onTap: () {
                        setState(() {
                          _obscure = !_obscure;
                        });
                      },
                      child: Icon(
                        _obscure
                            ? Icons.visibility_off_outlined
                            : Icons.visibility_outlined,
                        color: AppTheme.textLight,
                        size: 20,
                      ),
                    )
                  : widget.suffixIcon,
            ),
          ),
        ],
      );
}

// Custom Button
class GixatButton extends StatelessWidget {
  const GixatButton({
    required this.label,
    required this.onPressed,
    Key? key,
    this.isLoading = false,
    this.isEnabled = true,
    this.width,
    this.height = 56,
    this.backgroundColor,
    this.textColor,
    this.borderRadius = AppTheme.radiusLarge,
  }) : super(key: key);
  final String label;
  final VoidCallback onPressed;
  final bool isLoading;
  final bool isEnabled;
  final double? width;
  final double height;
  final Color? backgroundColor;
  final Color? textColor;
  final double borderRadius;

  @override
  Widget build(BuildContext context) => SizedBox(
        width: width,
        height: height,
        child: ElevatedButton(
          onPressed: isEnabled && !isLoading ? onPressed : null,
          style: ElevatedButton.styleFrom(
            backgroundColor: backgroundColor ?? AppTheme.primary,
            disabledBackgroundColor: AppTheme.border,
            foregroundColor: textColor ?? Colors.white,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(borderRadius),
            ),
            elevation: 0,
          ),
          child: isLoading
              ? SizedBox(
                  height: 24,
                  width: 24,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.5,
                    valueColor: AlwaysStoppedAnimation<Color>(
                        textColor ?? Colors.white),
                  ),
                )
              : Text(
                  label,
                  style: Theme.of(context).textTheme.labelLarge?.copyWith(
                        color: textColor ?? Colors.white,
                      ),
                ),
        ),
      );
}

// Custom Error Widget
class ErrorWidget extends StatelessWidget {
  const ErrorWidget({
    required this.message,
    Key? key,
    this.onRetry,
  }) : super(key: key);
  final String message;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(AppTheme.spacing24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(
                Icons.error_outline,
                size: 64,
                color: AppTheme.error,
              ),
              const SizedBox(height: AppTheme.spacing16),
              Text(
                message,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      color: AppTheme.textDark,
                    ),
              ),
              if (onRetry != null) ...[
                const SizedBox(height: AppTheme.spacing24),
                GixatButton(
                  label: 'Retry',
                  onPressed: onRetry!,
                  width: 120,
                ),
              ],
            ],
          ),
        ),
      );
}

// Loading Widget
class LoadingWidget extends StatelessWidget {
  const LoadingWidget({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) => const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppTheme.primary),
          strokeWidth: 3,
        ),
      );
}

// Snackbar Helper
void showGixatSnackbar(
  BuildContext context, {
  required String message,
  bool isError = false,
  Duration duration = const Duration(seconds: 3),
}) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(message),
      backgroundColor: isError ? AppTheme.error : AppTheme.success,
      duration: duration,
      behavior: SnackBarBehavior.floating,
      margin: const EdgeInsets.all(AppTheme.spacing16),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppTheme.radiusLarge),
      ),
    ),
  );
}
