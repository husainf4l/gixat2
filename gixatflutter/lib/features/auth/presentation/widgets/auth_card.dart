import 'package:flutter/material.dart';

class AuthCard extends StatelessWidget {
  final Widget child;
  final double width;
  final double? height;

  const AuthCard({
    Key? key,
    required this.child,
    this.width = 380,
    this.height,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Container(
      width: width,
      height: height,
      margin: const EdgeInsets.symmetric(horizontal: 20),
      padding: const EdgeInsets.fromLTRB(32, 50, 32, 40),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(32),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.06),
            blurRadius: 40,
            offset: const Offset(0, 20),
            spreadRadius: 0,
          ),
          BoxShadow(
            color: const Color(0xFF5EC8F2).withOpacity(0.08),
            blurRadius: 60,
            offset: const Offset(0, 30),
            spreadRadius: 0,
          ),
        ],
      ),
      child: child,
    );
  }
}
