import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:go_router/go_router.dart';

class AppLayout extends StatelessWidget {
  const AppLayout({
    required this.child,
    required this.currentPath,
    Key? key,
  }) : super(key: key);
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
      builder: (context) => Container(
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
              context,
              icon: FontAwesomeIcons.calendarPlus,
              label: 'New Session',
              onTap: () => Navigator.pop(context),
            ),
            _buildAddOption(
              context,
              icon: FontAwesomeIcons.fileSignature,
              label: 'New Job Card',
              onTap: () => Navigator.pop(context),
            ),
            _buildAddOption(
              context,
              icon: FontAwesomeIcons.userPlus,
              label: 'New Client',
              onTap: () => Navigator.pop(context),
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
      body: child,
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _getSelectedIndex(),
        onTap: (index) => _onItemTapped(context, index),
        type: BottomNavigationBarType.fixed,
        backgroundColor: Colors.white,
        selectedItemColor: const Color(0xFF1B75BC),
        unselectedItemColor: Colors.grey[400],
        showSelectedLabels: false,
        showUnselectedLabels: false,
        items: const [
          BottomNavigationBarItem(
            icon: FaIcon(FontAwesomeIcons.clock, size: 24),
            label: 'Sessions',
          ),
          BottomNavigationBarItem(
            icon: FaIcon(FontAwesomeIcons.fileLines, size: 24),
            label: 'Job Cards',
          ),
          BottomNavigationBarItem(
            icon: FaIcon(FontAwesomeIcons.circlePlus, size: 32),
            label: 'Add',
          ),
          BottomNavigationBarItem(
            icon: FaIcon(FontAwesomeIcons.users, size: 24),
            label: 'Clients',
          ),
          BottomNavigationBarItem(
            icon: FaIcon(FontAwesomeIcons.gear, size: 24),
            label: 'Settings',
          ),
        ],
      ),
    );
}
