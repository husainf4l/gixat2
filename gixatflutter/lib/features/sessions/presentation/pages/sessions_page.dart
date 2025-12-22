import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';
import 'package:go_router/go_router.dart';
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
          await context.push('/sessions/create');
          // Refresh sessions list after returning
          if (context.mounted) {
            context.read<SessionsCubit>().loadSessions(refresh: true);
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

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: InkWell(
        onTap: () {
          // Navigate to session details
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
