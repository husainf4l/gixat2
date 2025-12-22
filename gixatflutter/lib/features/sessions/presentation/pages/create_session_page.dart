import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import '../bloc/create_session_cubit.dart';

class CreateSessionPage extends StatefulWidget {
  const CreateSessionPage({super.key});

  @override
  State<CreateSessionPage> createState() => _CreateSessionPageState();
}

class _CreateSessionPageState extends State<CreateSessionPage> with RouteAware {
  String? selectedCustomerId;
  String? selectedCarId;

  @override
  void initState() {
    super.initState();
    context.read<CreateSessionCubit>().loadCustomers();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Subscribe to route changes
    final route = ModalRoute.of(context);
    if (route is PageRoute) {
      // Listen for when this route becomes active again
      route.didPush().then((_) {
        // Already handled in initState
      });
    }
  }

  void _refreshCustomers() {
    setState(() {
      selectedCustomerId = null;
      selectedCarId = null;
    });
    context.read<CreateSessionCubit>().loadCustomers();
  }

  Future<void> _navigateToCreateClient() async {
    await context.push('/clients/create');
    // Refresh customers list when returning from create client page
    if (mounted) {
      _refreshCustomers();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Create Session'),
      ),
      body: BlocConsumer<CreateSessionCubit, CreateSessionState>(
        listener: (context, state) {
          if (state is CreateSessionSuccess) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Session created successfully!')),
            );
            Navigator.pop(context, true); // Return true to indicate refresh needed
          } else if (state is CreateSessionError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Error: ${state.message}')),
            );
          }
        },
        builder: (context, state) {
          if (state is CreateSessionLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          final customers = (state is CreateSessionLoaded) 
              ? state.customers 
              : <Map<String, dynamic>>[];
          final cars = (state is CreateSessionLoaded) 
              ? state.cars 
              : <Map<String, dynamic>>[];

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Customer Selection
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Select Customer',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 16),
                        DropdownButtonFormField<String>(
                          value: selectedCustomerId,
                          decoration: const InputDecoration(
                            labelText: 'Customer',
                            border: OutlineInputBorder(),
                          ),
                          items: customers.map((customer) {
                            return DropdownMenuItem<String>(
                              value: customer['id'] as String,
                              child: Text(
                                '${customer['firstName']} ${customer['lastName']}',
                              ),
                            );
                          }).toList(),
                          onChanged: (value) {
                            setState(() {
                              selectedCustomerId = value;
                              selectedCarId = null; // Reset car selection
                            });
                            if (value != null) {
                              context.read<CreateSessionCubit>().loadCarsForCustomer(value);
                            }
                          },
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),

                // Car Selection
                if (selectedCustomerId != null)
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              const Text(
                                'Select Customer',
                                style: TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  IconButton(
                                    icon: const Icon(Icons.refresh),
                                    tooltip: 'Refresh customers',
                                    onPressed: _refreshCustomers,
                                  ),
                                  IconButton(
                                    icon: const Icon(Icons.add),
                                    tooltip: 'Create new customer',
                                    onPressed: _navigateToCreateClient,
                                  ),
                                ],
                              ),
                            ],
                          ),
                          const SizedBox(height: 8),
                          DropdownButtonFormField<String>(
                            value: selectedCarId,
                            decoration: const InputDecoration(
                              labelText: 'Car',
                              border: OutlineInputBorder(),
                            ),
                            items: cars.map((car) {
                              return DropdownMenuItem<String>(
                                value: car['id'] as String,
                                child: Text(
                                  '${car['make']} ${car['model']} - ${car['licensePlate']}',
                                ),
                              );
                            }).toList(),
                            onChanged: (value) {
                              setState(() {
                                selectedCarId = value;
                              });
                            },
                          ),
                        ],
                      ),
                    ),
                  ),

                const SizedBox(height: 24),

                // Create Button
                ElevatedButton(
                  onPressed: (selectedCustomerId != null && selectedCarId != null)
                      ? () {
                          context.read<CreateSessionCubit>().createSession(
                                customerId: selectedCustomerId!,
                                carId: selectedCarId!,
                              );
                        }
                      : null,
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.all(16),
                  ),
                  child: const Text(
                    'Create Session',
                    style: TextStyle(fontSize: 16),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
