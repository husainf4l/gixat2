import 'package:flutter/material.dart';

class AuthTextField extends StatelessWidget {
  const AuthTextField({
    required this.hint,
    required this.icon,
    Key? key,
    this.isPassword = false,
    this.controller,
    this.validator,
    this.keyboardType,
  }) : super(key: key);
  final String hint;
  final IconData icon;
  final bool isPassword;
  final TextEditingController? controller;
  final String? Function(String?)? validator;
  final TextInputType? keyboardType;

  @override
  Widget build(BuildContext context) => Container(
        decoration: BoxDecoration(
          color: const Color(0xFFF5F7FA),
          borderRadius: BorderRadius.circular(14),
        ),
        child: TextFormField(
          controller: controller,
          validator: validator,
          keyboardType: keyboardType,
          obscureText: isPassword,
          style: const TextStyle(
            fontSize: 15,
            color: Color(0xFF2C3E50),
            fontWeight: FontWeight.w500,
          ),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: TextStyle(
              fontSize: 15,
              color: Colors.grey[400],
              fontWeight: FontWeight.w400,
            ),
            prefixIcon: Icon(
              icon,
              color: const Color(0xFF1B75BC),
              size: 22,
            ),
            border: InputBorder.none,
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 20,
              vertical: 18,
            ),
          ),
        ),
      );
}
