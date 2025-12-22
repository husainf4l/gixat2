import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:go_router/go_router.dart';

class AppLayout extends StatelessWidget {
  const AppLayout({
    required this.child,
    required this.currentPath,
    super.key,
  });
  
  final Widget child;
  final String currentPath;

  int _getSelectedIndex() {
    if (currentPath.startsWith('/sessions')) {
      return 0;
    }
    if (currentPath.startsWith('/job-cards')) {
      return 1;
    }
    if (currentPath.startsWith('/clients')) {
      return 3;
    }
    if (currentPath.startsWith('/settings')) {
      return 4;
    }
    return 0;
  }

  void _onItemTapped(BuildContext context, int index) {
    switch (index) {
      case 0:
        context.go('/sessions');
        break;
      case 1:
        context.go('/job-cards');
        break;
      case 2:
        _showAddOptions(context);
        break;
      case 3:
        context.go('/clients');
        break;
      case 4:
        context.go('/settings');
        break;
    }
  }

  void _showAddOptions(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (modalContext) => Container(
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: Colors.grey[300],
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(height: 24),
            const Text(
              'Create New',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 24),
            _buildAddOption(
              modalContext,
              icon: FontAwesomeIcons.calendarPlus,
              label: 'New Session',
              onTap: () {
                Navigator.pop(modalContext);
                context.push('/sessions/create');
              },
            ),
            _buildAddOption(
              modalContext,
              icon: FontAwesomeIcons.fileSignature,
              label: 'New Job Card',
              onTap: () {
                Navigator.pop(modalContext);
                context.push('/job-cards/create');
              },
            ),
            _buildAddOption(
              modalContext,
              icon: FontAwesomeIcons.userPlus,
              label: 'New Client',
              onTap: () {
                Navigator.pop(modalContext);
                context.push('/clients/create');
              },
            ),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }

  Widget _buildAddOption(
    BuildContext context, {
    required IconData icon,
    required String label,
    required VoidCallback onTap,
  }) => ListTile(
      leading: FaIcon(icon, color: const Color(0xFF1B75BC)),
      title: Text(label),
      onTap: onTap,
    );

  @override
  Widget build(BuildContext context) => Scaffold(
      body: RepaintBoundary(child: child),
      bottomNavigationBar: RepaintBoundary(
        child: Container(
          decoration: BoxDecoration(
            color: Colors.white,
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.05),
                blurRadius: 20,
                offset: const Offset(0, -5),
              ),
            ],
          ),
          child: SafeArea(
            child: Container(
              height: 50,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  _buildNavItem(
                    context,
                    icon: FontAwesomeIcons.clock,
                    index: 0,
                    isSelected: _getSelectedIndex() == 0,
                  ),
                  _buildNavItem(
                    context,
                    icon: FontAwesomeIcons.fileLines,
                    index: 1,
                    isSelected: _getSelectedIndex() == 1,
                  ),
                  _buildAddButton(context),
                  _buildNavItem(
                    context,
                    icon: FontAwesomeIcons.users,
                    index: 3,
                    isSelected: _getSelectedIndex() == 3,
                  ),
                  _buildNavItem(
                    context,
                    icon: FontAwesomeIcons.gear,
                    index: 4,
                    isSelected: _getSelectedIndex() == 4,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );

  Widget _buildNavItem(
    BuildContext context, {
    required IconData icon,
    required int index,
    required bool isSelected,
  }) =>
      GestureDetector(
        onTap: () => _onItemTapped(context, index),
        child: Container(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            color: isSelected
                ? const Color(0xFF1B75BC).withValues(alpha: 0.1)
                : Colors.transparent,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Center(
            child: FaIcon(
              icon,
              size: 18,
              color: isSelected
                  ? const Color(0xFF1B75BC)
                  : Colors.grey[400],
            ),
          ),
        ),
      );

  Widget _buildAddButton(BuildContext context) => InkWell(
        onTap: () => _showAddOptions(context),
        splashColor: Colors.transparent,
        highlightColor: Colors.transparent,
        child: Container(
          width: 42,
          height: 42,
          decoration: BoxDecoration(
            gradient: const LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                Color(0xFF1B75BC),
                Color(0xFF1557A0),
              ],
            ),
            borderRadius: BorderRadius.circular(12),
            boxShadow: [
              BoxShadow(
                color: const Color(0xFF1B75BC).withValues(alpha: 0.3),
                blurRadius: 8,
                offset: const Offset(0, 3),
              ),
            ],
          ),
          child: const Center(
            child: FaIcon(
              FontAwesomeIcons.plus,
              size: 18,
              color: Colors.white,
            ),
          ),
        ),
      );
}
