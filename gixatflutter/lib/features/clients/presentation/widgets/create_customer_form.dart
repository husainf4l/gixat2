import 'package:flutter/material.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import '../../../../core/graphql/customer_mutations.dart';
import '../../../../core/graphql/lookup_queries.dart';

class CreateCustomerForm extends StatefulWidget {
  const CreateCustomerForm({Key? key}) : super(key: key);

  @override
  State<CreateCustomerForm> createState() => _CreateCustomerFormState();
}

class _CreateCustomerFormState extends State<CreateCustomerForm> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _streetController = TextEditingController();
  bool _isLoading = false;
  
  // Lookup state
  String? _selectedCountry;
  String? _selectedCity;
  List<Map<String, dynamic>> _countries = [];
  List<Map<String, dynamic>> _cities = [];
  
  // Cache parsed GraphQL documents
  static final _createMutationDoc = gql(createCustomerMutation);
  static final _countriesQueryDoc = gql(getCountriesWithCitiesQuery);
  
  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _streetController.dispose();
    super.dispose();
  }

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() => _isLoading = true);

    try {
      final client = GraphQLProvider.of(context).value;

      // Split name into first and last
      final nameParts = _nameController.text.trim().split(' ');
      final firstName = nameParts.first;
      final lastName = nameParts.length > 1 ? nameParts.sublist(1).join(' ') : '';

      final result = await client.mutate(
        MutationOptions(
          document: _createMutationDoc,
          variables: {
            'input': {
              'firstName': firstName,
              'lastName': lastName,
              'phoneNumber': _phoneController.text.trim(),
              if (_emailController.text.trim().isNotEmpty)
                'email': _emailController.text.trim(),
              if (_selectedCountry != null) 'country': _selectedCountry,
              if (_selectedCity != null) 'city': _selectedCity,
              if (_streetController.text.trim().isNotEmpty)
                'street': _streetController.text.trim(),
            },
          },
        ),
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _isLoading = false;
      });

      if (result.hasException) {
        _showErrorSnackbar(
          result.exception?.graphqlErrors.first.message ??
              'Failed to create customer',
        );
        return;
      }

      Navigator.pop(context, true);
      _showSuccessSnackbar('Customer created successfully!');
    } on Exception catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _isLoading = false;
      });
      _showErrorSnackbar('An error occurred: ${e.toString()}');
    }
  }

  void _showSuccessSnackbar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.2),
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Icon(
                Icons.check_circle_outline,
                color: Colors.white,
                size: 20,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                message,
                style: const TextStyle(fontSize: 14),
              ),
            ),
          ],
        ),
        backgroundColor: const Color(0xFF10B981),
        behavior: SnackBarBehavior.floating,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(12)),
        ),
        margin: const EdgeInsets.all(16),
      ),
    );
  }

  void _showErrorSnackbar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.2),
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Icon(
                Icons.error_outline,
                color: Colors.white,
                size: 20,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                message,
                style: const TextStyle(fontSize: 14),
              ),
            ),
          ],
        ),
        backgroundColor: const Color(0xFFEF4444),
        behavior: SnackBarBehavior.floating,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(12)),
        ),
        margin: const EdgeInsets.all(16),
      ),
    );
  }

  @override
  Widget build(BuildContext context) => Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: IconButton(
          onPressed: () => Navigator.pop(context),
          icon: const Icon(Icons.close_rounded),
          color: const Color(0xFF6B7280),
        ),
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: const Color(0xFF1B75BC).withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Icon(
                Icons.person_add_rounded,
                color: Color(0xFF1B75BC),
                size: 20,
              ),
            ),
            const SizedBox(width: 12),
            const Text(
              'New Customer',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w700,
                color: Color(0xFF1A1A2E),
              ),
            ),
          ],
        ),
      ),
      body: SafeArea(
        child: Query(
          options: QueryOptions(
            document: _countriesQueryDoc,
            fetchPolicy: FetchPolicy.cacheFirst,
          ),
          builder: (result, {fetchMore, refetch}) {
            if (result.isLoading && result.data == null) {
              return const Center(child: CircularProgressIndicator());
            }

            // Extract countries data outside of build
            // to avoid setState during build
            var countriesData = <Map<String, dynamic>>[];
            if (result.data != null) {
              final items = result.data!['lookupItems'] as List;
              countriesData = items
                  .map((item) => {
                        'value': item['value'] as String,
                        'children': item['children'] as List,
                      })
                  .toList();
              
              // Update state after build completes
              if (_countries.isEmpty && countriesData.isNotEmpty) {
                WidgetsBinding.instance.addPostFrameCallback((_) {
                  if (mounted) {
                    setState(() {
                      _countries = countriesData;
                    });
                  }
                });
              }
            }

            return Form(
              key: _formKey,
              child: Column(
                children: [
                  Expanded(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        children: [
                          _buildTextField(
                            controller: _nameController,
                            label: 'Full Name',
                            hint: 'Enter customer name',
                            icon: Icons.person_outline_rounded,
                            validator: (value) {
                              if (value?.isEmpty ?? true) {
                                return 'Name is required';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 20),
                          _buildTextField(
                            controller: _phoneController,
                            label: 'Phone Number',
                            hint: '+962 7X XXX XXXX',
                            icon: Icons.phone_outlined,
                            keyboardType: TextInputType.phone,
                            validator: (value) {
                              if (value?.isEmpty ?? true) {
                                return 'Phone number is required';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 20),
                          _buildTextField(
                            controller: _emailController,
                            label: 'Email (Optional)',
                            hint: 'customer@example.com',
                            icon: Icons.email_outlined,
                            keyboardType: TextInputType.emailAddress,
                          ),
                          const SizedBox(height: 20),
                          if (_countries.isNotEmpty)
                            _buildDropdown(
                              label: 'Country (Optional)',
                              hint: 'Select country',
                              icon: Icons.flag_outlined,
                              value: _selectedCountry,
                              items: _countries
                                  .map((country) => country['value'] as String)
                                  .toList(),
                              onChanged: (value) {
                              setState(() {
                                _selectedCountry = value;
                                _selectedCity = null;
                                final selected = _countries.firstWhere(
                                  (c) => c['value'] == value,
                                );
                                _cities = (selected['children'] as List)
                                    .map((city) => {
                                          'value': city['value'] as String,
                                        })
                                    .toList();
                              });
                            },
                          ),
                          if (_selectedCountry != null) ...[
                            const SizedBox(height: 20),
                            _buildDropdown(
                              label: 'City (Optional)',
                              hint: 'Select city',
                              icon: Icons.location_city_outlined,
                              value: _selectedCity,
                              items: _cities
                                  .map((city) => city['value'] as String)
                                  .toList(),
                              onChanged: (value) {
                                setState(() => _selectedCity = value);
                              },
                            ),
                          ],
                          const SizedBox(height: 20),
                          _buildTextField(
                            controller: _streetController,
                            label: 'Street Address (Optional)',
                            hint: 'Street name and number',
                            icon: Icons.home_outlined,
                            maxLines: 2,
                          ),
                        ],
                      ),
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.all(24),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.05),
                          blurRadius: 10,
                          offset: const Offset(0, -5),
                        ),
                      ],
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: OutlinedButton(
                            onPressed: () => Navigator.pop(context),
                            style: OutlinedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(vertical: 16),
                              side: BorderSide(
                                color: Colors.grey[300]!,
                                width: 1.5,
                              ),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                            ),
                            child: const Text(
                              'Cancel',
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w600,
                                color: Color(0xFF6B7280),
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          flex: 2,
                          child: ElevatedButton(
                            onPressed: _isLoading ? null : _submitForm,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFF1B75BC),
                              foregroundColor: Colors.white,
                              disabledBackgroundColor:
                                  const Color(0xFF1B75BC).withValues(alpha: 0.5),
                              padding: const EdgeInsets.symmetric(vertical: 16),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                              elevation: 0,
                            ),
                            child: _isLoading
                                ? const SizedBox(
                                    width: 20,
                                    height: 20,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      valueColor: AlwaysStoppedAnimation<Color>(
                                        Colors.white,
                                      ),
                                    ),
                                  )
                                : const Text(
                                    'Create Customer',
                                    style: TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _buildDropdown({
    required String label,
    required String hint,
    required IconData icon,
    required String? value,
    required List<String> items,
    required void Function(String?) onChanged,
  }) {
    // Safety check for empty items
    if (items.isEmpty) {
      return const SizedBox.shrink();
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            color: Color(0xFF1A1A2E),
          ),
        ),
        const SizedBox(height: 8),
        Container(
          decoration: BoxDecoration(
            color: const Color(0xFFF9FAFB),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: Colors.grey[300]!,
              width: 1,
            ),
          ),
          child: DropdownButtonFormField<String>(
            initialValue: value,
            isExpanded: true,
            hint: Text(
              hint,
              style: TextStyle(
                color: Colors.grey[400],
                fontSize: 14,
              ),
            ),
            icon: const Icon(
              Icons.keyboard_arrow_down_rounded,
              color: Color(0xFF6B7280),
            ),
            decoration: InputDecoration(
              prefixIcon: Icon(
                icon,
                color: const Color(0xFF6B7280),
                size: 20,
              ),
              border: InputBorder.none,
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 16,
                vertical: 14,
              ),
            ),
            items: items.map((item) => DropdownMenuItem<String>(
              value: item,
              child: Text(item),
            )).toList(),
            onChanged: onChanged,
          ),
        ),
      ],
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    TextInputType? keyboardType,
    int maxLines = 1,
    String? Function(String?)? validator,
  }) =>
      Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            color: Color(0xFF1A1A2E),
          ),
        ),
        const SizedBox(height: 8),
        TextFormField(
          controller: controller,
          keyboardType: keyboardType,
          maxLines: maxLines,
          validator: validator,
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: TextStyle(
              color: Colors.grey[400],
              fontSize: 14,
            ),
            prefixIcon: Icon(
              icon,
              color: const Color(0xFF6B7280),
              size: 20,
            ),
            filled: true,
            fillColor: const Color(0xFFF9FAFB),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: BorderSide(
                color: Colors.grey[300]!,
                width: 1,
              ),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: BorderSide(
                color: Colors.grey[300]!,
                width: 1,
              ),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(
                color: Color(0xFF1B75BC),
                width: 2,
              ),
            ),
            errorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(
                color: Color(0xFFEF4444),
                width: 1,
              ),
            ),
            focusedErrorBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(
                color: Color(0xFFEF4444),
                width: 2,
              ),
            ),
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 16,
              vertical: 14,
            ),
          ),
        ),
      ],
    );
  }
}
