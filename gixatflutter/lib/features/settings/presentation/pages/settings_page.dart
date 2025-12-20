import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../auth/presentation/bloc/auth_cubit.dart';

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
      appBar: AppBar(
        title: const Text('Settings'),
        elevation: 0,
        backgroundColor: Colors.transparent,
        foregroundColor: Colors.black,
      ),
      body: ListView(
        children: [
          const SizedBox(height: 20),
          _buildSection(
            'Account',
            [
              _buildTile(
                context,
                icon: CupertinoIcons.person,
                title: 'Profile Details',
                onTap: () {},
              ),
              _buildTile(
                context,
                icon: CupertinoIcons.lock,
                title: 'Security',
                onTap: () {},
              ),
            ],
          ),
          _buildSection(
            'Business',
            [
              _buildTile(
                context,
                icon: CupertinoIcons.briefcase,
                title: 'Garage Info',
                onTap: () {},
              ),
              _buildTile(
                context,
                icon: CupertinoIcons.person_3,
                title: 'Team Members',
                onTap: () {},
              ),
            ],
          ),
          _buildSection(
            'App',
            [
              _buildTile(
                context,
                icon: CupertinoIcons.bell,
                title: 'Notifications',
                onTap: () {},
              ),
              _buildTile(
                context,
                icon: CupertinoIcons.info,
                title: 'About Gixat',
                onTap: () {},
              ),
            ],
          ),
          const SizedBox(height: 32),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: ElevatedButton(
              onPressed: () {
                context.read<AuthCubit>().logout();
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.redAccent.withValues(alpha: 0.1),
                foregroundColor: Colors.redAccent,
                elevation: 0,
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              child: const Text(
                'Sign Out',
                style: TextStyle(fontWeight: FontWeight.bold),
              ),
            ),
          ),
          const SizedBox(height: 40),
        ],
      ),
    );

  Widget _buildSection(String title, List<Widget> children) => Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: Text(
            title.toUpperCase(),
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.bold,
              color: Colors.grey[500],
              letterSpacing: 1.2,
            ),
          ),
        ),
        ...children,
      ],
    );

  Widget _buildTile(
    BuildContext context, {
    required IconData icon,
    required String title,
    required VoidCallback onTap,
  }) => ListTile(
      leading: Icon(icon, color: const Color(0xFF1B75BC), size: 22),
      title: Text(
        title,
        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
      ),
      trailing: const Icon(
        CupertinoIcons.chevron_right,
        size: 18,
        color: Colors.grey,
      ),
      onTap: onTap,
    );
}
