import 'package:flutter/material.dart';
import 'sidebar.dart';

class AppLayout extends StatelessWidget {
  final Widget child;
  final String currentPath;

  const AppLayout({
    Key? key,
    required this.child,
    required this.currentPath,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
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
}
