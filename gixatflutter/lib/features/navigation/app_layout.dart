import 'package:flutter/material.dart';
import 'sidebar.dart';

class AppLayout extends StatelessWidget {
  const AppLayout({
    required this.child,
    required this.currentPath,
    Key? key,
  }) : super(key: key);
  final Widget child;
  final String currentPath;

  @override
  Widget build(BuildContext context) => Scaffold(
        drawer: Drawer(
          width: 260,
          backgroundColor: Colors.transparent,
          child: AppSidebar(currentPath: currentPath),
        ),
        body: Container(
          color: const Color(0xFFF5F7FA),
          child: child,
        ),
      );
}
