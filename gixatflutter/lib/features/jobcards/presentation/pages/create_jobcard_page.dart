import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/create_jobcard_cubit.dart';

class CreateJobCardPage extends StatefulWidget {
  const CreateJobCardPage({super.key});

  @override
  State<CreateJobCardPage> createState() => _CreateJobCardPageState();
}

class _CreateJobCardPageState extends State<CreateJobCardPage> {
  final _formKey = GlobalKey<FormState>();
  final _descriptionController = TextEditingController();
  final _estimatedCostController = TextEditingController();
  String? selectedSessionId;

  @override
  void initState() {
    super.initState();
    context.read<CreateJobCardCubit>().loadSessions();
  }

  void _refreshSessions() {
    setState(() {
      selectedSessionId = null;
    });
    context.read<CreateJobCardCubit>().loadSessions();
  }

  @override
  void dispose() {
    _descriptionController.dispose();
    _estimatedCostController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Create Job Card'),
      ),
      body: BlocConsumer<CreateJobCardCubit, CreateJobCardState>(
        listener: (context, state) {
          if (state is CreateJobCardSuccess) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Job card created successfully!')),
            );
            Navigator.pop(context, true);
          } else if (state is CreateJobCardError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Error: ${state.message}')),
            );
          }
        },
        builder: (context, state) {
          if (state is CreateJobCardLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          final sessions = (state is CreateJobCardLoaded) 
              ? state.sessions 
              : <Map<String, dynamic>>[];

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16.0),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Session Selection
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
                                'Select Session',
                                style: TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              IconButton(
                                icon: const Icon(Icons.refresh),
                                tooltip: 'Refresh sessions',
                                onPressed: _refreshSessions,
                              ),
                            ],
                          ),
                          const SizedBox(height: 8),
                          DropdownButtonFormField<String>(
                            value: selectedSessionId,
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Session',
                              border: OutlineInputBorder(),
                              contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 16),
                            ),
                            validator: (value) {
                              if (value == null || value.isEmpty) {
                                return 'Please select a session';
                              }
                              return null;
                            },
                            items: sessions.map((session) {
                              final customer = session['customer'] as Map<String, dynamic>;
                              final car = session['car'] as Map<String, dynamic>;
                              return DropdownMenuItem<String>(
                                value: session['id'] as String,
                                child: Row(
                                  children: [
                                    Expanded(
                                      child: Text(
                                        '${customer['firstName']} ${customer['lastName']} - ${car['make']} ${car['model']}',
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ),
                                  ],
                                ),
                              );
                            }).toList(),
                            onChanged: (value) {
                              setState(() {
                                selectedSessionId = value;
                              });
                            },
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Description
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Job Details',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 16),
                          TextFormField(
                            controller: _descriptionController,
                            decoration: const InputDecoration(
                              labelText: 'Description',
                              hintText: 'Enter job description',
                              border: OutlineInputBorder(),
                            ),
                            maxLines: 4,
                          ),
                          const SizedBox(height: 16),
                          TextFormField(
                            controller: _estimatedCostController,
                            decoration: const InputDecoration(
                              labelText: 'Estimated Cost',
                              hintText: 'Enter estimated cost',
                              border: OutlineInputBorder(),
                              prefixText: '\$ ',
                            ),
                            keyboardType: TextInputType.numberWithOptions(decimal: true),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Create Button
                  ElevatedButton(
                    onPressed: (selectedSessionId != null)
                        ? () {
                            if (_formKey.currentState!.validate()) {
                              context.read<CreateJobCardCubit>().createJobCard(
                                    sessionId: selectedSessionId!,
                                    description: _descriptionController.text.trim().isNotEmpty
                                        ? _descriptionController.text.trim()
                                        : null,
                                    estimatedCost: _estimatedCostController.text.trim().isNotEmpty
                                        ? double.tryParse(_estimatedCostController.text.trim())
                                        : null,
                                  );
                            }
                          }
                        : null,
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.all(16),
                    ),
                    child: const Text(
                      'Create Job Card',
                      style: TextStyle(fontSize: 16),
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
}
