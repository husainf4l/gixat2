import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class AppTheme {
  // Colors
  static const Color primary = Color(0xFF1B75BC); // Gixat Blue
  static const Color primaryDark = Color(0xFF155A91);
  static const Color secondary = Color(0xFF64748B); // Slate
  static const Color background = Color(0xFFF8FAFC);
  static const Color surface = Color(0xFFFFFFFF);
  static const Color error = Color(0xFFDC2626);
  static const Color success = Color(0xFF16A34A);
  static const Color textDark = Color(0xFF1E293B);
  static const Color textLight = Color(0xFF64748B);
  static const Color border = Color(0xFFE2E8F0);
  static const Color divider = Color(0xFFF1F5F9);

  // Spacing
  static const double spacing8 = 8;
  static const double spacing12 = 12;
  static const double spacing16 = 16;
  static const double spacing20 = 20;
  static const double spacing24 = 24;
  static const double spacing32 = 32;

  // Border Radius
  static const double radiusSmall = 8;
  static const double radiusMedium = 12;
  static const double radiusLarge = 16;

  static ThemeData get lightTheme => ThemeData(
        useMaterial3: true,
        brightness: Brightness.light,
        scaffoldBackgroundColor: background,
        colorScheme: const ColorScheme.light(
          primary: primary,
          secondary: secondary,
          error: error,
          surface: surface,
          onPrimary: Colors.white,
          onSecondary: Colors.white,
          onError: Colors.white,
          onSurface: textDark,
        ),
        textTheme: GoogleFonts.interTextTheme(
          const TextTheme(
            displayLarge: TextStyle(
              fontSize: 32,
              fontWeight: FontWeight.w700,
              color: textDark,
              height: 1.2,
            ),
            displayMedium: TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.w700,
              color: textDark,
              height: 1.2,
            ),
            displaySmall: TextStyle(
              fontSize: 24,
              fontWeight: FontWeight.w700,
              color: textDark,
              height: 1.2,
            ),
            headlineLarge: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w600,
              color: textDark,
              height: 1.4,
            ),
            headlineMedium: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w600,
              color: textDark,
              height: 1.4,
            ),
            headlineSmall: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              color: textDark,
              height: 1.4,
            ),
            titleLarge: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w500,
              color: textDark,
              height: 1.5,
            ),
            titleMedium: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w500,
              color: textDark,
              height: 1.5,
            ),
            titleSmall: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w500,
              color: textDark,
              height: 1.5,
            ),
            bodyLarge: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w400,
              color: textDark,
              height: 1.5,
            ),
            bodyMedium: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w400,
              color: textDark,
              height: 1.5,
            ),
            bodySmall: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w400,
              color: textLight,
              height: 1.4,
            ),
            labelLarge: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w500,
              color: Colors.white,
              height: 1.4,
            ),
            labelMedium: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w500,
              color: Colors.white,
              height: 1.4,
            ),
            labelSmall: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w500,
              color: Colors.white,
              height: 1.3,
            ),
          ),
        ),
        appBarTheme: AppBarTheme(
          backgroundColor: background,
          foregroundColor: textDark,
          elevation: 0,
          centerTitle: true,
          titleTextStyle: GoogleFonts.inter(
            fontSize: 18,
            fontWeight: FontWeight.w600,
            color: textDark,
          ),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: spacing16,
            vertical: spacing14,
          ),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(radiusLarge),
            borderSide: const BorderSide(color: border, width: 1.5),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(radiusLarge),
            borderSide: const BorderSide(color: border, width: 1.5),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(radiusLarge),
            borderSide: const BorderSide(color: primary, width: 2),
          ),
          errorBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(radiusLarge),
            borderSide: const BorderSide(color: error, width: 1.5),
          ),
          focusedErrorBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(radiusLarge),
            borderSide: const BorderSide(color: error, width: 2),
          ),
          hintStyle: const TextStyle(
            fontSize: 14,
            color: textLight,
            fontWeight: FontWeight.w400,
          ),
          errorStyle: const TextStyle(
            fontSize: 12,
            color: error,
            fontWeight: FontWeight.w500,
          ),
          labelStyle: GoogleFonts.inter(
            fontSize: 14,
            fontWeight: FontWeight.w500,
            color: textDark,
          ),
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            backgroundColor: primary,
            foregroundColor: Colors.white,
            elevation: 0,
            padding: const EdgeInsets.symmetric(
              horizontal: spacing24,
              vertical: spacing14,
            ),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(radiusLarge),
            ),
            textStyle: GoogleFonts.inter(
              fontSize: 16,
              fontWeight: FontWeight.w600,
              height: 1.5,
            ),
          ),
        ),
        textButtonTheme: TextButtonThemeData(
          style: TextButton.styleFrom(
            foregroundColor: primary,
            textStyle: GoogleFonts.inter(
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
      );
}

// Define spacing as constants for easy reference
const double spacing14 = 14;
