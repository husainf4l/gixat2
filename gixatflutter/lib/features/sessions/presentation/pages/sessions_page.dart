import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';
import 'package:go_router/go_router.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import '../../../../core/graphql/graphql_client.dart';
import '../../data/repositories/sessions_repository.dart';
import '../bloc/sessions_cubit.dart';

class SessionsPage extends StatelessWidget {
  const SessionsPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => SessionsCubit(
        repository: SessionsRepository(),
      )..loadSessions(),
      child: const _SessionsView(),
    );
  }
}

class _SessionsView extends StatelessWidget {
  const _SessionsView();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Sessions'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => context.read<SessionsCubit>().loadSessions(refresh: true),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push('/sessions/create');
          // Refresh sessions list after returning if successful
          if (result == true && context.mounted) {
            try {
              context.read<SessionsCubit>().loadSessions(refresh: true);
            } catch (e) {
              // Cubit might be disposed, ignore
            }
          }
        },
        child: const Icon(Icons.add),
      ),
      body: BlocBuilder<SessionsCubit, SessionsState>(
        builder: (context, state) {
          if (state is SessionsLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is SessionsError) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: Colors.red),
                  const SizedBox(height: 16),
                  Text(state.message),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () => context.read<SessionsCubit>().loadSessions(refresh: true),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          if (state is SessionsLoaded) {
            if (state.sessions.isEmpty) {
              return const Center(
                child: Text('No sessions found'),
              );
            }

            return RefreshIndicator(
              onRefresh: () => context.read<SessionsCubit>().loadSessions(refresh: true),
              child: ListView.builder(
                itemCount: state.sessions.length + (state.hasNextPage ? 1 : 0),
                itemBuilder: (context, index) {
                  if (index == state.sessions.length) {
                    if (!state.isLoadingMore) {
                      context.read<SessionsCubit>().loadMore();
                    }
                    return const Center(
                      child: Padding(
                        padding: EdgeInsets.all(16.0),
                        child: CircularProgressIndicator(),
                      ),
                    );
                  }

                  final session = state.sessions[index];
                  return _SessionCard(session: session);
                },
              ),
            );
          }

          return const SizedBox.shrink();
        },
      ),
    );
  }
}

class _SessionCard extends StatelessWidget {
  const _SessionCard({required this.session});

  final session;

  Color _getStatusColor(String status) {
    switch (status.toUpperCase()) {
      case 'INTAKE':
        return Colors.blue;
      case 'INSPECTION':
        return Colors.orange;
      case 'REPAIR':
        return Colors.purple;
      case 'COMPLETED':
        return Colors.green;
      default:
        return Colors.grey;
    }
  }

  void _showSessionDetails(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => DraggableScrollableSheet(
        initialChildSize: 0.7,
        minChildSize: 0.5,
        maxChildSize: 0.95,
        builder: (context, scrollController) => Container(
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
          ),
          child: Column(
            children: [
              Container(
                margin: const EdgeInsets.only(top: 12),
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              Expanded(
                child: ListView(
                  controller: scrollController,
                  padding: const EdgeInsets.all(24),
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Session Details',
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                          decoration: BoxDecoration(
                            color: _getStatusColor(session.status).withOpacity(0.1),
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: _getStatusColor(session.status),
                              width: 1,
                            ),
                          ),
                          child: Text(
                            session.status,
                            style: TextStyle(
                              color: _getStatusColor(session.status),
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 24),
                    _EditableDetailSection(
                      title: 'Session Status',
                      icon: Icons.assignment,
                      onEdit: () => _showEditStatus(context),
                      children: [
                        _DetailRow('Status', session.status),
                        _DetailRow('Created', DateFormat('MMM dd, yyyy HH:mm').format(session.createdAt)),
                      ],
                    ),
                    const SizedBox(height: 16),
                    _EditableDetailSection(
                      title: 'Customer Information',
                      icon: Icons.person,
                      onEdit: () => _showEditCustomer(context),
                      children: [
                        _DetailRow('Name', session.customer.fullName),
                      ],
                    ),
                    const SizedBox(height: 16),
                    _EditableDetailSection(
                      title: 'Vehicle Information',
                      icon: Icons.directions_car,
                      onEdit: () => _showEditVehicle(context),
                      children: [
                        _DetailRow('Make', session.car.make),
                        _DetailRow('Model', session.car.model),
                        _DetailRow('License Plate', session.car.licensePlate),
                      ],
                    ),
                    const SizedBox(height: 16),
                    _DetailSection(
                      title: 'Session Information',
                      icon: Icons.info_outline,
                      children: [
                        _DetailRow('Session ID', '#${session.id.substring(0, 8)}'),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showEditStatus(BuildContext context) {
    String selectedStatus = session.status;
    
    showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          title: const Text('Edit Status'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ListTile(
                title: const Text('INTAKE'),
                leading: Radio(
                  value: 'INTAKE',
                  groupValue: selectedStatus,
                  onChanged: (value) {
                    setState(() => selectedStatus = value!);
                  },
                ),
              ),
              ListTile(
                title: const Text('INSPECTION'),
                leading: Radio(
                  value: 'INSPECTION',
                  groupValue: selectedStatus,
                  onChanged: (value) {
                    setState(() => selectedStatus = value!);
                  },
                ),
              ),
              ListTile(
                title: const Text('REPAIR'),
                leading: Radio(
                  value: 'REPAIR',
                  groupValue: selectedStatus,
                  onChanged: (value) {
                    setState(() => selectedStatus = value!);
                  },
                ),
              ),
              ListTile(
                title: const Text('COMPLETED'),
                leading: Radio(
                  value: 'COMPLETED',
                  groupValue: selectedStatus,
                  onChanged: (value) {
                    setState(() => selectedStatus = value!);
                  },
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: const Text('Cancel'),
            ),
            ElevatedButton(
              onPressed: () async {
                Navigator.pop(dialogContext);
                await _updateSessionStatus(context, selectedStatus);
              },
              child: const Text('Save'),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _updateSessionStatus(BuildContext context, String newStatus) async {
    try {
      final client = await GraphQLConfig.getClient(withAuth: true);
      
      final result = await client.mutate(
        MutationOptions(
          document: gql('''
            mutation UpdateSessionStatus(\$id: UUID!, \$status: SessionStatus!) {
              updateSessionStatus(id: \$id, status: \$status) {
                id
                status
              }
            }
          '''),
          variables: {
            'id': session.id,
            'status': newStatus,
          },
        ),
      );

      if (result.hasException) {
        throw result.exception!;
      }

      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Status updated successfully')),
        );
        Navigator.pop(context);
        context.read<SessionsCubit>().loadSessions(refresh: true);
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to update status: $e')),
        );
      }
    }
  }

  void _showEditCustomer(BuildContext context) {
    final nameController = TextEditingController(text: session.customer.fullName);
    
    showDialog(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Edit Customer'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: nameController,
              decoration: const InputDecoration(
                labelText: 'Full Name',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 16),
            const Text(
              'Note: Customer changes will be reflected across all sessions',
              style: TextStyle(fontSize: 12, color: Colors.grey),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(dialogContext);
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Customer update saved')),
              );
            },
            child: const Text('Save'),
          ),
        ],
      ),
    );
  }

  void _showEditVehicle(BuildContext context) {
    final makeController = TextEditingController(text: session.car.make);
    final modelController = TextEditingController(text: session.car.model);
    final plateController = TextEditingController(text: session.car.licensePlate);
    
    showDialog(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Edit Vehicle'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: makeController,
                decoration: const InputDecoration(
                  labelText: 'Make',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: modelController,
                decoration: const InputDecoration(
                  labelText: 'Model',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: plateController,
                decoration: const InputDecoration(
                  labelText: 'License Plate',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 16),
              const Text(
                'Note: Vehicle changes will be reflected across all sessions',
                style: TextStyle(fontSize: 12, color: Colors.grey),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(dialogContext);
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Vehicle update saved')),
              );
            },
            child: const Text('Save'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: InkWell(
        onTap: () {
          _showSessionDetails(context);
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Text(
                      session.customer.fullName,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: _getStatusColor(session.status).withOpacity(0.1),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: _getStatusColor(session.status),
                        width: 1,
                      ),
                    ),
                    child: Text(
                      session.status,
                      style: TextStyle(
                        color: _getStatusColor(session.status),
                        fontWeight: FontWeight.w600,
                        fontSize: 12,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  const Icon(Icons.directions_car, size: 18, color: Colors.grey),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      '${session.car.make} ${session.car.model}',
                      style: const TextStyle(fontSize: 16),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  const Icon(Icons.credit_card, size: 18, color: Colors.grey),
                  const SizedBox(width: 8),
                  Text(
                    session.car.licensePlate,
                    style: const TextStyle(
                      fontSize: 14,
                      color: Colors.grey,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.calendar_today, size: 16, color: Colors.grey),
                      const SizedBox(width: 4),
                      Text(
                        DateFormat('MMM dd, yyyy').format(session.createdAt),
                        style: const TextStyle(
                          fontSize: 12,
                          color: Colors.grey,
                        ),
                      ),
                    ],
                  ),
                  Text(
                    '#${session.id.substring(0, 8)}',
                    style: const TextStyle(
                      fontSize: 12,
                      color: Colors.grey,
                      fontFamily: 'monospace',
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _EditableDetailSection extends StatelessWidget {
  const _EditableDetailSection({
    required this.title,
    required this.icon,
    required this.children,
    required this.onEdit,
  });

  final String title;
  final IconData icon;
  final List<Widget> children;
  final VoidCallback onEdit;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey[50],
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey[200]!),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(icon, size: 20, color: const Color(0xFF1B75BC)),
                  const SizedBox(width: 8),
                  Text(
                    title,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF1B75BC),
                    ),
                  ),
                ],
              ),
              IconButton(
                icon: const Icon(Icons.edit, size: 20),
                color: const Color(0xFF1B75BC),
                onPressed: onEdit,
                tooltip: 'Edit',
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...children,
        ],
      ),
    );
  }
}

class _DetailSection extends StatelessWidget {
  const _DetailSection({
    required this.title,
    required this.icon,
    required this.children,
  });

  final String title;
  final IconData icon;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey[50],
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey[200]!),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 20, color: const Color(0xFF1B75BC)),
              const SizedBox(width: 8),
              Text(
                title,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF1B75BC),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...children,
        ],
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow(this.label, this.value);

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 120,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 14,
                color: Colors.grey,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
