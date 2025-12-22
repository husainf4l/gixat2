import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/create_car_cubit.dart';

class CreateCarPage extends StatefulWidget {
  final String customerId;
  final String customerName;

  const CreateCarPage({
    super.key,
    required this.customerId,
    required this.customerName,
  });

  @override
  State<CreateCarPage> createState() => _CreateCarPageState();
}

class _CreateCarPageState extends State<CreateCarPage> {
  final _formKey = GlobalKey<FormState>();
  final _makeController = TextEditingController();
  final _modelController = TextEditingController();
  final _yearController = TextEditingController();
  final _licensePlateController = TextEditingController();
  final _vinController = TextEditingController();
  final _colorController = TextEditingController();

  @override
  void dispose() {
    _makeController.dispose();
    _modelController.dispose();
    _yearController.dispose();
    _licensePlateController.dispose();
    _vinController.dispose();
    _colorController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Add Car'),
      ),
      body: BlocConsumer<CreateCarCubit, CreateCarState>(
        listener: (context, state) {
          if (state is CreateCarSuccess) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Car added successfully!')),
            );
            Navigator.pop(context, true);
          } else if (state is CreateCarError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Error: ${state.message}')),
            );
          }
        },
        builder: (context, state) {
          final isLoading = state is CreateCarLoading;

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16.0),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Customer: ${widget.customerName}',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 8),
                          const Text(
                            'Add vehicle information',
                            style: TextStyle(
                              fontSize: 14,
                              color: Colors.grey,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Vehicle Details',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 16),

                          TextFormField(
                            controller: _makeController,
                            decoration: const InputDecoration(
                              labelText: 'Make *',
                              hintText: 'e.g., Toyota, BMW, Ford',
                              border: OutlineInputBorder(),
                            ),
                            validator: (value) {
                              if (value == null || value.trim().isEmpty) {
                                return 'Please enter vehicle make';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 16),

                          TextFormField(
                            controller: _modelController,
                            decoration: const InputDecoration(
                              labelText: 'Model *',
                              hintText: 'e.g., Camry, X5, F-150',
                              border: OutlineInputBorder(),
                            ),
                            validator: (value) {
                              if (value == null || value.trim().isEmpty) {
                                return 'Please enter vehicle model';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 16),

                          TextFormField(
                            controller: _yearController,
                            decoration: const InputDecoration(
                              labelText: 'Year',
                              hintText: 'e.g., 2023',
                              border: OutlineInputBorder(),
                            ),
                            keyboardType: TextInputType.number,
                          ),
                          const SizedBox(height: 16),

                          TextFormField(
                            controller: _licensePlateController,
                            decoration: const InputDecoration(
                              labelText: 'License Plate *',
                              hintText: 'e.g., ABC-1234',
                              border: OutlineInputBorder(),
                            ),
                            validator: (value) {
                              if (value == null || value.trim().isEmpty) {
                                return 'Please enter license plate';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 16),

                          TextFormField(
                            controller: _vinController,
                            decoration: const InputDecoration(
                              labelText: 'VIN',
                              hintText: 'Vehicle Identification Number',
                              border: OutlineInputBorder(),
                            ),
                          ),
                          const SizedBox(height: 16),

                          TextFormField(
                            controller: _colorController,
                            decoration: const InputDecoration(
                              labelText: 'Color',
                              hintText: 'e.g., White, Black, Red',
                              border: OutlineInputBorder(),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),

                  ElevatedButton(
                    onPressed: isLoading
                        ? null
                        : () {
                            if (_formKey.currentState!.validate()) {
                              context.read<CreateCarCubit>().createCar(
                                    customerId: widget.customerId,
                                    make: _makeController.text.trim(),
                                    model: _modelController.text.trim(),
                                    year: _yearController.text.trim().isNotEmpty
                                        ? int.tryParse(_yearController.text.trim())
                                        : null,
                                    licensePlate: _licensePlateController.text.trim(),
                                    vin: _vinController.text.trim().isNotEmpty
                                        ? _vinController.text.trim()
                                        : null,
                                    color: _colorController.text.trim().isNotEmpty
                                        ? _colorController.text.trim()
                                        : null,
                                  );
                            }
                          },
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.all(16),
                    ),
                    child: isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text(
                            'Add Car',
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
