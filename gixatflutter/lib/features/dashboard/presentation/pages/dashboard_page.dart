import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../data/models/dashboard_data.dart';
import '../../data/repositories/dashboard_repository.dart';
import '../bloc/dashboard_cubit.dart';

class DashboardPage extends StatelessWidget {
  const DashboardPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) => BlocProvider(
        create: (context) => DashboardCubit(
          repository: DashboardRepository(),
        )..loadDashboard(),
        child: const _DashboardView(),
      );
}

class _DashboardView extends StatelessWidget {
  const _DashboardView();

  @override
  Widget build(BuildContext context) =>
      BlocBuilder<DashboardCubit, DashboardState>(
        builder: (context, state) {
          if (state is DashboardLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is DashboardError) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.error_outline, size: 48, color: Colors.red[300]),
                  const SizedBox(height: 16),
                  Text('Error: ${state.message}'),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () =>
                        context.read<DashboardCubit>().loadDashboard(),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          final data = state is DashboardLoaded ? state.data : null;
          final stats = data?.stats ?? DashboardStats.empty();

          return SafeArea(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Page Header
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          _HeaderButton(
                            icon: Icons.menu_rounded,
                            onPressed: () {
                              Scaffold.of(context).openDrawer();
                            },
                            tooltip: 'Menu',
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Text(
                              'Dashboard',
                              style: TextStyle(
                                fontSize: 28,
                                fontWeight: FontWeight.w700,
                                color: Color(0xFF1A1A2E),
                                letterSpacing: -0.5,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(
                            child: Text(
                              'Today\'s overview • '
                              '${_formatDate(DateTime.now())}',
                              style: const TextStyle(
                                fontSize: 14,
                                color: Color(0xFF6B7280),
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const SizedBox(width: 8),
                          _HeaderButton(
                            icon: Icons.refresh_rounded,
                            onPressed: () {
                              context.read<DashboardCubit>().refreshDashboard();
                            },
                            tooltip: 'Refresh',
                          ),
                          const SizedBox(width: 8),
                          _HeaderButton(
                            icon: Icons.notifications_rounded,
                            onPressed: () {},
                            tooltip: 'Notifications',
                          ),
                        ],
                      ),
                    ],
                  ),

                  const SizedBox(height: 24),

                  // Quick Stats
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final crossAxisCount = constraints.maxWidth > 1200
                          ? 4
                          : constraints.maxWidth > 800
                              ? 2
                              : 1;
                      const spacing = 16.0;
                      final itemWidth = (constraints.maxWidth -
                              (spacing * (crossAxisCount - 1))) /
                          crossAxisCount;

                      return Wrap(
                        spacing: spacing,
                        runSpacing: spacing,
                        children: [
                          SizedBox(
                            width: itemWidth,
                            child: _StatCard(
                              label: 'Today\'s Sessions',
                              value: '${stats.todaySessions}',
                              icon: Icons.access_time_rounded,
                              color: const Color(0xFF6366F1),
                              trend: stats.todaySessions > 0
                                  ? '+${stats.todaySessions}'
                                  : '0',
                              trendUp: stats.todaySessions > 0,
                            ),
                          ),
                          SizedBox(
                            width: itemWidth,
                            child: _StatCard(
                              label: 'Active Job Cards',
                              value: '${stats.activeJobCards}',
                              icon: Icons.build_rounded,
                              color: const Color(0xFF8B5CF6),
                              trend: stats.activeJobCards > 0
                                  ? '+${stats.activeJobCards}'
                                  : '0',
                              trendUp: stats.activeJobCards > 0,
                            ),
                          ),
                          SizedBox(
                            width: itemWidth,
                            child: _StatCard(
                              label: 'Pending Appointments',
                              value: '${stats.pendingAppointments}',
                              icon: Icons.calendar_today_rounded,
                              color: const Color(0xFFF59E0B),
                              trend: stats.pendingAppointments > 0
                                  ? '${stats.pendingAppointments}'
                                  : '0',
                              trendUp: false,
                            ),
                          ),
                          SizedBox(
                            width: itemWidth,
                            child: _StatCard(
                              label: 'Cars In Garage',
                              value: '${stats.carsInGarage}',
                              icon: Icons.directions_car_rounded,
                              color: const Color(0xFF10B981),
                              trend: stats.carsInGarage > 0
                                  ? '+${stats.carsInGarage}'
                                  : '0',
                              trendUp: stats.carsInGarage > 0,
                            ),
                          ),
                        ],
                      );
                    },
                  ),

                  const SizedBox(height: 24),

                  // Today's Schedule and Active Job Cards
                  LayoutBuilder(
                    builder: (context, constraints) {
                      if (constraints.maxWidth > 900) {
                        return Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: _SectionCard(
                                title: 'Today\'s Schedule',
                                subtitle: stats.pendingAppointments > 0
                                    ? '${stats.pendingAppointments} '
                                        'appointments'
                                    : 'No appointments',
                                child: const _EmptyState(
                                  icon: Icons.calendar_today_rounded,
                                  message: 'No appointments scheduled '
                                      'for today',
                                  description: 'Appointments will appear '
                                      'here when available',
                                ),
                              ),
                            ),
                            const SizedBox(width: 16),
                            Expanded(
                              child: _SectionCard(
                                title: 'Active Job Cards',
                                subtitle: stats.activeJobCards > 0
                                    ? '${stats.activeJobCards} in progress'
                                    : 'No active jobs',
                                child: const _EmptyState(
                                  icon: Icons.build_rounded,
                                  message: 'No active job cards',
                                  description: 'Job cards will appear '
                                      'here when created',
                                ),
                              ),
                            ),
                          ],
                        );
                      } else {
                        return Column(
                          children: [
                            _SectionCard(
                              title: 'Today\'s Schedule',
                              subtitle: stats.pendingAppointments > 0
                                  ? '${stats.pendingAppointments} '
                                      'appointments'
                                  : 'No appointments',
                              child: const _EmptyState(
                                icon: Icons.calendar_today_rounded,
                                message: 'No appointments scheduled '
                                    'for today',
                                description: 'Appointments will appear '
                                    'here when available',
                              ),
                            ),
                            const SizedBox(height: 16),
                            _SectionCard(
                              title: 'Active Job Cards',
                              subtitle: stats.activeJobCards > 0
                                  ? '${stats.activeJobCards} in progress'
                                  : 'No active jobs',
                              child: const _EmptyState(
                                icon: Icons.build_rounded,
                                message: 'No active job cards',
                                description: 'Job cards will appear '
                                    'here when created',
                              ),
                            ),
                          ],
                        );
                      }
                    },
                  ),

                  const SizedBox(height: 16),

                  // Alerts
                  const _SectionCard(
                    title: 'Attention Required',
                    subtitle: 'All clear',
                    child: _EmptyState(
                      icon: Icons.check_circle_outline_rounded,
                      message: 'All caught up!',
                      description:
                          'No items require your attention at the moment',
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      );

  String _formatDate(DateTime date) {
    final months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec'
    ];
    return '${months[date.month - 1]} ${date.day}, ${date.year}';
  }
}

class _HeaderButton extends StatelessWidget {
  const _HeaderButton({
    required this.icon,
    required this.onPressed,
    required this.tooltip,
  });
  final IconData icon;
  final VoidCallback onPressed;
  final String tooltip;

  @override
  Widget build(BuildContext context) => Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.05),
              blurRadius: 10,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: IconButton(
          icon: Icon(icon, size: 20),
          onPressed: onPressed,
          color: const Color(0xFF6B7280),
          tooltip: tooltip,
          constraints: const BoxConstraints(minWidth: 40, minHeight: 40),
          padding: EdgeInsets.zero,
        ),
      );
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
    this.trend,
    this.trendUp,
  });
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  final String? trend;
  final bool? trendUp;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFE5E7EB), width: 1),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.03),
              blurRadius: 10,
              offset: const Offset(0, 1),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(icon, color: color, size: 22),
                ),
                if (trend != null && trendUp != null)
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: (trendUp!
                              ? const Color(0xFF10B981)
                              : const Color(0xFFEF4444))
                          .withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          trendUp!
                              ? Icons.trending_up_rounded
                              : Icons.trending_down_rounded,
                          size: 14,
                          color: trendUp!
                              ? const Color(0xFF10B981)
                              : const Color(0xFFEF4444),
                        ),
                        const SizedBox(width: 4),
                        Text(
                          trend!,
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: trendUp!
                                ? const Color(0xFF10B981)
                                : const Color(0xFFEF4444),
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 16),
            Text(
              value,
              style: const TextStyle(
                fontSize: 32,
                fontWeight: FontWeight.w700,
                color: Color(0xFF1A1A2E),
                height: 1,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              label,
              style: const TextStyle(
                fontSize: 14,
                color: Color(0xFF6B7280),
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      );
}

class _SectionCard extends StatelessWidget {
  const _SectionCard({
    required this.title,
    required this.child,
    this.subtitle,
  });
  final String title;
  final String? subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) => Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFE5E7EB), width: 1),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.03),
              blurRadius: 10,
              offset: const Offset(0, 1),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.all(20),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                          color: Color(0xFF1A1A2E),
                        ),
                      ),
                      if (subtitle != null)
                        Padding(
                          padding: const EdgeInsets.only(top: 4),
                          child: Text(
                            subtitle!,
                            style: const TextStyle(
                              fontSize: 13,
                              color: Color(0xFF6B7280),
                            ),
                          ),
                        ),
                    ],
                  ),
                ],
              ),
            ),
            child,
          ],
        ),
      );
}

// Empty state widget for sections without data
class _EmptyState extends StatelessWidget {
  const _EmptyState({
    required this.icon,
    required this.message,
    required this.description,
  });

  final IconData icon;
  final String message;
  final String description;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF6366F1).withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Icon(
                icon,
                size: 40,
                color: const Color(0xFF6366F1).withValues(alpha: 0.6),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              message,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w600,
                color: Color(0xFF1A1A2E),
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              description,
              style: const TextStyle(
                fontSize: 14,
                color: Color(0xFF6B7280),
              ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      );
}
