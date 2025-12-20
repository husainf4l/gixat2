import 'package:flutter/material.dart';

class AuthCard extends StatelessWidget {
  final Widget child;
  final double? maxWidth;
  final EdgeInsetsGeometry? padding;

  const AuthCard({
    Key? key,
    required this.child,
    this.maxWidth = 440,
    this.padding,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Container(
        constraints: BoxConstraints(maxWidth: maxWidth!),
        margin: const EdgeInsets.symmetric(horizontal: 24),
        padding: padding ??
            const EdgeInsets.symmetric(
              horizontal: 32,
              vertical: 40,
            ),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(22),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.08),
              blurRadius: 30,
              offset: const Offset(0, 15),
              spreadRadius: 0,
            ),
            BoxShadow(
              color: const Color(0xFF6DD5FA).withOpacity(0.1),
              blurRadius: 60,
              offset: const Offset(0, 20),
              spreadRadius: 0,
            ),
          ],
        ),
        child: child,
      ),
    );
  }
}
