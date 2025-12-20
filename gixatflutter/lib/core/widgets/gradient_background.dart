import 'package:flutter/material.dart';

class GradientBackground extends StatelessWidget {
  const GradientBackground({
    required this.child,
    Key? key,
  }) : super(key: key);
  final Widget child;

  @override
  Widget build(BuildContext context) => Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [
              Color(0xFF4FA3E0), // Lighter Gixat Blue
              Color(0xFF1B75BC), // Gixat Blue
              Color(0xFF155A91), // Darker Gixat Blue
            ],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        child: child,
      );
}
