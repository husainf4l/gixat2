import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/dashboard_cubit.dart';
import '../../data/repositories/dashboard_repository.dart';

class DashboardPage extends StatelessWidget {
  const DashboardPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => DashboardCubit(
        repository: DashboardRepository(),
      )..loadDashboard(),
      child: const _DashboardView(),
    );
  }
}

class _DashboardView extends StatelessWidget {
  const _DashboardView();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F7FA),
      body: BlocBuilder<DashboardCubit, DashboardState>(
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
                    onPressed: () => context.read<DashboardCubit>().loadDashboard(),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          // final data = state is DashboardLoaded ? state.data : null;

          return SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
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
                          'Today\'s overview • ${_formatDate(DateTime.now())}',
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
                        badge: 3,
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
                  final spacing = 16.0;
                  final itemWidth = (constraints.maxWidth - (spacing * (crossAxisCount - 1))) / crossAxisCount;
                  
                  return Wrap(
                    spacing: spacing,
                    runSpacing: spacing,
                    children: [
                      SizedBox(
                        width: itemWidth,
                        child: const _StatCard(
                          label: 'Today\'s Sessions',
                          value: '12',
                          icon: Icons.access_time_rounded,
                          color: Color(0xFF6366F1),
                          trend: '+12%',
                          trendUp: true,
                        ),
                      ),
                      SizedBox(
                        width: itemWidth,
                        child: const _StatCard(
                          label: 'Active Job Cards',
                          value: '8',
                          icon: Icons.build_rounded,
                          color: Color(0xFF8B5CF6),
                          trend: '+5%',
                          trendUp: true,
                        ),
                      ),
                      SizedBox(
                        width: itemWidth,
                        child: const _StatCard(
                          label: 'Pending Appointments',
                          value: '5',
                          icon: Icons.calendar_today_rounded,
                          color: Color(0xFFF59E0B),
                          trend: '-3%',
                          trendUp: false,
                        ),
                      ),
                      SizedBox(
                        width: itemWidth,
                        child: const _StatCard(
                          label: 'Cars In Garage',
                          value: '15',
                          icon: Icons.directions_car_rounded,
                          color: Color(0xFF10B981),
                          trend: '+8%',
                          trendUp: true,
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
                            subtitle: '3 appointments',
                            child: Column(
                              children: [
                                const _ScheduleItem(
                                  time: '09:00 AM',
                                  clientName: 'John Doe',
                                  vehicle: 'Toyota Camry',
                                  plate: 'ABC123',
                                  status: 'Confirmed',
                                  statusColor: Color(0xFF10B981),
                                ),
                                const Divider(height: 1, thickness: 1),
                                const _ScheduleItem(
                                  time: '10:30 AM',
                                  clientName: 'Jane Smith',
                                  vehicle: 'Honda Civic',
                                  plate: 'XYZ789',
                                  status: 'Pending',
                                  statusColor: Color(0xFFF59E0B),
                                ),
                                const Divider(height: 1, thickness: 1),
                                const _ScheduleItem(
                                  time: '02:00 PM',
                                  clientName: 'Mike Johnson',
                                  vehicle: 'Ford F-150',
                                  plate: 'DEF456',
                                  status: 'In Progress',
                                  statusColor: Color(0xFF6366F1),
                                ),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: _SectionCard(
                            title: 'Active Job Cards',
                            subtitle: '8 in progress',
                            child: Column(
                              children: [
                                const _JobCardRow(
                                  jobNumber: '#J001',
                                  clientName: 'Sarah Williams',
                                  vehicle: 'BMW X5',
                                  status: 'In Progress',
                                  mechanic: 'Alex Brown',
                                  progress: 0.7,
                                  statusColor: Color(0xFF6366F1),
                                ),
                                const Divider(height: 1, thickness: 1),
                                const _JobCardRow(
                                  jobNumber: '#J002',
                                  clientName: 'Tom Davis',
                                  vehicle: 'Mercedes C300',
                                  status: 'Waiting Parts',
                                  mechanic: 'Chris Lee',
                                  progress: 0.4,
                                  statusColor: Color(0xFFF59E0B),
                                ),
                                const Divider(height: 1, thickness: 1),
                                const _JobCardRow(
                                  jobNumber: '#J003',
                                  clientName: 'Emily Chen',
                                  vehicle: 'Audi A4',
                                  status: 'Quality Check',
                                  mechanic: 'David Kim',
                                  progress: 0.9,
                                  statusColor: Color(0xFF8B5CF6),
                                ),
                              ],
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
                          subtitle: '3 appointments',
                          child: Column(
                            children: [
                              const _ScheduleItem(
                                time: '09:00 AM',
                                clientName: 'John Doe',
                                vehicle: 'Toyota Camry',
                                plate: 'ABC123',
                                status: 'Confirmed',
                                statusColor: Color(0xFF10B981),
                              ),
                              const Divider(height: 1, thickness: 1),
                              const _ScheduleItem(
                                time: '10:30 AM',
                                clientName: 'Jane Smith',
                                vehicle: 'Honda Civic',
                                plate: 'XYZ789',
                                status: 'Pending',
                                statusColor: Color(0xFFF59E0B),
                              ),
                              const Divider(height: 1, thickness: 1),
                              const _ScheduleItem(
                                time: '02:00 PM',
                                clientName: 'Mike Johnson',
                                vehicle: 'Ford F-150',
                                plate: 'DEF456',
                                status: 'In Progress',
                                statusColor: Color(0xFF6366F1),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),
                        _SectionCard(
                          title: 'Active Job Cards',
                          subtitle: '8 in progress',
                          child: Column(
                            children: [
                              const _JobCardRow(
                                jobNumber: '#J001',
                                clientName: 'Sarah Williams',
                                vehicle: 'BMW X5',
                                status: 'In Progress',
                                mechanic: 'Alex Brown',
                                progress: 0.7,
                                statusColor: Color(0xFF6366F1),
                              ),
                              const Divider(height: 1, thickness: 1),
                              const _JobCardRow(
                                jobNumber: '#J002',
                                clientName: 'Tom Davis',
                                vehicle: 'Mercedes C300',
                                status: 'Waiting Parts',
                                mechanic: 'Chris Lee',
                                progress: 0.4,
                                statusColor: Color(0xFFF59E0B),
                              ),
                              const Divider(height: 1, thickness: 1),
                              const _JobCardRow(
                                jobNumber: '#J003',
                                clientName: 'Emily Chen',
                                vehicle: 'Audi A4',
                                status: 'Quality Check',
                                mechanic: 'David Kim',
                                progress: 0.9,
                                statusColor: Color(0xFF8B5CF6),
                              ),
                            ],
                          ),
                        ),
                      ],
                    );
                  }
                },
              ),

              const SizedBox(height: 16),

              // Alerts
              _SectionCard(
                title: 'Attention Required',
                subtitle: '6 items',
                child: Column(
                  children: [
                    const _AlertItem(
                      icon: Icons.warning_rounded,
                      message: '2 jobs waiting for approval',
                      action: 'Review',
                      color: Color(0xFFF59E0B),
                    ),
                    const Divider(height: 1, thickness: 1),
                    const _AlertItem(
                      icon: Icons.error_rounded,
                      message: '1 overdue job card',
                      action: 'View',
                      color: Color(0xFFEF4444),
                    ),
                    const Divider(height: 1, thickness: 1),
                    const _AlertItem(
                      icon: Icons.inventory_rounded,
                      message: '3 items low in inventory',
                      action: 'Check',
                      color: Color(0xFFF59E0B),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      );
        },
      ),
    );
  }

  static String _formatDate(DateTime date) {
    final months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return '${months[date.month - 1]} ${date.day}, ${date.year}';
  }
}

class _HeaderButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback onPressed;
  final String tooltip;
  final int? badge;

  const _HeaderButton({
    required this.icon,
    required this.onPressed,
    required this.tooltip,
    this.badge,
  });

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Container(
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.05),
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
        ),
        if (badge != null && badge! > 0)
          Positioned(
            right: 4,
            top: 4,
            child: Container(
              padding: const EdgeInsets.all(4),
              decoration: const BoxDecoration(
                color: Color(0xFFEF4444),
                shape: BoxShape.circle,
              ),
              constraints: const BoxConstraints(
                minWidth: 16,
                minHeight: 16,
              ),
              child: Text(
                badge! > 9 ? '9+' : badge.toString(),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 9,
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
              ),
            ),
          ),
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  final String? trend;
  final bool? trendUp;

  const _StatCard({
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
    this.trend,
    this.trendUp,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE5E7EB), width: 1),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.03),
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
                  color: color.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, color: color, size: 22),
              ),
              if (trend != null && trendUp != null)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: (trendUp! ? const Color(0xFF10B981) : const Color(0xFFEF4444)).withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        trendUp! ? Icons.trending_up_rounded : Icons.trending_down_rounded,
                        size: 14,
                        color: trendUp! ? const Color(0xFF10B981) : const Color(0xFFEF4444),
                      ),
                      const SizedBox(width: 4),
                      Text(
                        trend!,
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: trendUp! ? const Color(0xFF10B981) : const Color(0xFFEF4444),
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
}

class _SectionCard extends StatelessWidget {
  final String title;
  final String? subtitle;
  final Widget child;

  const _SectionCard({
    required this.title,
    this.subtitle,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE5E7EB), width: 1),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.03),
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
}

class _ScheduleItem extends StatelessWidget {
  final String time;
  final String clientName;
  final String vehicle;
  final String plate;
  final String status;
  final Color statusColor;

  const _ScheduleItem({
    required this.time,
    required this.clientName,
    required this.vehicle,
    required this.plate,
    required this.status,
    required this.statusColor,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {},
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFF6366F1).withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Icon(
                  Icons.schedule_rounded,
                  size: 20,
                  color: Color(0xFF6366F1),
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      clientName,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF1A1A2E),
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '$vehicle • $plate',
                      style: const TextStyle(
                        fontSize: 13,
                        color: Color(0xFF6B7280),
                      ),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(
                          Icons.access_time_rounded,
                          size: 14,
                          color: Color(0xFF9CA3AF),
                        ),
                        const SizedBox(width: 4),
                        Text(
                          time,
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color(0xFF9CA3AF),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: statusColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  status,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: statusColor,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _JobCardRow extends StatelessWidget {
  final String jobNumber;
  final String clientName;
  final String vehicle;
  final String status;
  final String mechanic;
  final double progress;
  final Color statusColor;

  const _JobCardRow({
    required this.jobNumber,
    required this.clientName,
    required this.vehicle,
    required this.status,
    required this.mechanic,
    required this.progress,
    required this.statusColor,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {},
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                        decoration: BoxDecoration(
                          color: statusColor.withOpacity(0.1),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          jobNumber,
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: statusColor,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: statusColor.withOpacity(0.1),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          status,
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                            color: statusColor,
                          ),
                        ),
                      ),
                    ],
                  ),
                  Text(
                    '${(progress * 100).toInt()}%',
                    style: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFF1A1A2E),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                '$clientName • $vehicle',
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF1A1A2E),
                ),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  const Icon(Icons.person_rounded, size: 14, color: Color(0xFF9CA3AF)),
                  const SizedBox(width: 4),
                  Text(
                    mechanic,
                    style: const TextStyle(
                      fontSize: 13,
                      color: Color(0xFF6B7280),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(
                  value: progress,
                  backgroundColor: const Color(0xFFE5E7EB),
                  valueColor: AlwaysStoppedAnimation<Color>(statusColor),
                  minHeight: 6,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AlertItem extends StatelessWidget {
  final IconData icon;
  final String message;
  final String action;
  final Color color;

  const _AlertItem({
    required this.icon,
    required this.message,
    required this.action,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {},
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: color.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, color: color, size: 20),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Text(
                  message,
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                    color: Color(0xFF1A1A2E),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                decoration: BoxDecoration(
                  color: color,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  action,
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: Colors.white,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
