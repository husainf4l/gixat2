import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
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
              icon: CupertinoIcons.calendar_badge_plus,
              label: 'New Session',
              onTap: () => Navigator.pop(context),
            ),
            _buildAddOption(
              context,
              icon: CupertinoIcons.doc_on_doc,
              label: 'New Job Card',
              onTap: () => Navigator.pop(context),
            ),
            _buildAddOption(
              context,
              icon: CupertinoIcons.person_add,
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
      leading: Icon(icon, color: const Color(0xFF6366F1)),
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
        selectedItemColor: const Color(0xFF6366F1),
        unselectedItemColor: Colors.grey[400],
        showSelectedLabels: true,
        showUnselectedLabels: true,
        items: const [
          BottomNavigationBarItem(
            icon: Icon(CupertinoIcons.time),
            label: 'Sessions',
          ),
          BottomNavigationBarItem(
            icon: Icon(CupertinoIcons.doc_text),
            label: 'Job Cards',
          ),
          BottomNavigationBarItem(
            icon: Icon(CupertinoIcons.add_circled_solid, size: 32),
            label: 'Add',
          ),
          BottomNavigationBarItem(
            icon: Icon(CupertinoIcons.person_2),
            label: 'Clients',
          ),
          BottomNavigationBarItem(
            icon: Icon(CupertinoIcons.settings),
            label: 'Settings',
          ),
        ],
      ),
    );
}
