import 'dart:async';
import 'package:flutter/material.dart';

/// A premium animated logo splash for "GIXAT".
class GixatLogoSplash extends StatefulWidget {
  const GixatLogoSplash({
    super.key,
    this.onFinished,
    this.duration = const Duration(milliseconds: 1800),
    this.holdAfter = const Duration(milliseconds: 400),
    this.logoText = 'GIXAT',
  });

  final VoidCallback? onFinished;
  final Duration duration;
  final Duration holdAfter;
  final String logoText;

  @override
  State<GixatLogoSplash> createState() => _GixatLogoSplashState();
}

class _GixatLogoSplashState extends State<GixatLogoSplash>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<double> _scaleAnimation;
  late final Animation<double> _fadeAnimation;
  late final Animation<double> _rotationAnimation;
  late final Animation<double> _glowAnimation;

  @override
  void initState() {
    super.initState();

    _controller = AnimationController(
      vsync: this,
      duration: widget.duration,
    )..repeat(reverse: true); // Continuous bouncing animation

    // Dramatic scale bounce (0.85 to 1.1)
    _scaleAnimation = Tween<double>(begin: 0.85, end: 1.1).animate(
      CurvedAnimation(
        parent: _controller,
        curve: Curves.elasticInOut,
      ),
    );

    // Pulsing opacity (0.7 to 1.0)
    _fadeAnimation = Tween<double>(begin: 0.7, end: 1).animate(
      CurvedAnimation(
        parent: _controller,
        curve: Curves.easeInOut,
      ),
    );

    // Rotation wiggle (-0.05 to 0.05 radians = about -3° to +3°)
    _rotationAnimation = Tween<double>(begin: -0.05, end: 0.05).animate(
      CurvedAnimation(
        parent: _controller,
        curve: Curves.easeInOut,
      ),
    );

    // Dramatic glow pulse (0.3 to 1.0)
    _glowAnimation = Tween<double>(begin: 0.3, end: 1).animate(
      CurvedAnimation(
        parent: _controller,
        curve: Curves.easeInOut,
      ),
    );

    // Trigger callback after animation
    Future.delayed(widget.duration + widget.holdAfter, () {
      if (!mounted) return;
      widget.onFinished?.call();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: Colors.white,
        body: Center(
          child: AnimatedBuilder(
            animation: _controller,
            builder: (context, _) => Transform.scale(
              scale: _scaleAnimation.value,
              child: Transform.rotate(
                angle: _rotationAnimation.value,
                child: Opacity(
                  opacity: _fadeAnimation.value,
                  child: Container(
                    width: 200,
                    height: 200,
                    padding: const EdgeInsets.all(28),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(45),
                      boxShadow: [
                        // Base shadow
                        BoxShadow(
                          color: Colors.black.withOpacity(0.12),
                          blurRadius: 30,
                          offset: const Offset(0, 15),
                          spreadRadius: 3,
                        ),
                        // Primary animated glow - cyan
                        BoxShadow(
                          color: const Color(0xFF5EC8F2)
                              .withOpacity(0.6 * _glowAnimation.value),
                          blurRadius: 50 * _glowAnimation.value,
                          offset: const Offset(0, 0),
                          spreadRadius: 8 * _glowAnimation.value,
                        ),
                        // Secondary glow - blue
                        BoxShadow(
                          color: const Color(0xFF3B9FD9)
                              .withOpacity(0.4 * _glowAnimation.value),
                          blurRadius: 35 * _glowAnimation.value,
                          offset: const Offset(0, 5),
                          spreadRadius: 4 * _glowAnimation.value,
                        ),
                      ],
                    ),
                    child: Image.asset(
                      'assets/images/gixat-logo.png',
                      fit: BoxFit.contain,
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      );
}
