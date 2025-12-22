import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import '../bloc/clients_cubit.dart';

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
  bool _isLoadingCountries = false;

  // Lookup state
  String? _selectedCountry;
  String? _selectedCity;
  List<Map<String, dynamic>> _countries = [];
  List<Map<String, dynamic>> _cities = [];

  // Phone metadata
  String _phonePrefix = '';
  int _phoneLength = 0;
  bool _hasInitialized = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Fetch countries after dependencies are available
    if (!_hasInitialized) {
      _hasInitialized = true;
      _fetchCountries();
    }
  }

  Future<void> _fetchCountries() async {
    setState(() => _isLoadingCountries = true);

    try {
      final countries = await context.read<ClientsCubit>().getCountries();

      if (!mounted) {
        return;
      }

      setState(() {
        _countries = countries;
        
        // Set UAE as default country
        final uaeCountry = countries.firstWhere(
          (c) => c['value'] == 'United Arab Emirates',
          orElse: () => <String, dynamic>{},
        );
        
        if (uaeCountry.isNotEmpty) {
          _selectedCountry = uaeCountry['value'] as String;
          
          // Parse UAE metadata for phone code
          _parseCountryMetadata(uaeCountry['metadata'] as String?);
          
          // Load UAE cities
          _cities = (uaeCountry['children'] as List)
              .map((city) => {
                    'value': city['value'] as String,
                  })
              .toList();
        }
      });
    } on Exception catch (e) {
      if (kDebugMode) {
        print('Error fetching countries: $e');
      }
    } finally {
      if (mounted) {
        setState(() => _isLoadingCountries = false);
      }
    }
  }

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

    try {
      // Split name into first and last
      final nameParts = _nameController.text.trim().split(' ');
      final firstName = nameParts.first;
      final lastName =
          nameParts.length > 1 ? nameParts.sublist(1).join(' ') : '';

      if (kDebugMode) {
        print('Creating customer with:');
        print('  firstName: $firstName');
        print('  lastName: $lastName');
        print('  phoneNumber: ${_phoneController.text.trim()}');
        print('  email: ${_emailController.text.trim()}');
        print('  country: $_selectedCountry');
        print('  city: $_selectedCity');
        print('  street: ${_streetController.text.trim()}');
      }

      await context.read<ClientsCubit>().createCustomer(
            firstName: firstName,
            lastName: lastName,
            phoneNumber: _phoneController.text.trim(),
            email: _emailController.text.trim().isNotEmpty
                ? _emailController.text.trim()
                : null,
            country: _selectedCountry,
            city: _selectedCity,
            street: _streetController.text.trim().isNotEmpty
                ? _streetController.text.trim()
                : null,
          );
    } catch (e, stackTrace) {
      if (kDebugMode) {
        print('ERROR in _submitForm: $e');
        print('Stack trace: $stackTrace');
      }
      _showErrorSnackbar('Error creating customer: $e');
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

  void _parseCountryMetadata(String? metadata) {
    if (metadata == null || metadata.isEmpty) {
      setState(() {
        _phonePrefix = '';
        _phoneLength = 0;
      });
      return;
    }

    try {
      final data = jsonDecode(metadata) as Map<String, dynamic>;
      setState(() {
        _phonePrefix = data['phoneCode'] as String? ?? '';
        _phoneLength = data['phoneLength'] as int? ?? 0;
      });
    } on Exception {
      setState(() {
        _phonePrefix = '';
        _phoneLength = 0;
      });
    }
  }

  String? _validatePhoneNumber(String? value) {
    if (value?.isEmpty ?? true) {
      return 'Phone number is required';
    }

    // Remove spaces and special characters for length validation
    final cleanNumber = value!.replaceAll(RegExp(r'[\s\-\(\)]'), '');

    if (_phoneLength > 0 && cleanNumber.length != _phoneLength) {
      return 'Phone number must be $_phoneLength digits';
    }

    // Basic validation: only numbers, spaces, and common separators
    if (!RegExp(r'^[\d\s\-\(\)\+]+$').hasMatch(value)) {
      return 'Invalid phone number format';
    }

    return null;
  }

  @override
  Widget build(BuildContext context) =>
      BlocListener<ClientsCubit, ClientsState>(
        listener: (context, state) {
          if (state is CreateCustomerSuccess) {
            // Navigate to add car page with customer details
            final customerId = state.customerId;
            final customerName = _nameController.text.trim();
            
            // Pop the current page first
            Navigator.pop(context, true);
            
            // Navigate to add car page
            context.push('/clients/add-car?customerId=$customerId&customerName=${Uri.encodeComponent(customerName)}');
            
            _showSuccessSnackbar('Customer created! Now add their car');
          } else if (state is ClientsError) {
            _showErrorSnackbar(state.message);
          }
        },
        child: BlocBuilder<ClientsCubit, ClientsState>(
          builder: (context, state) {
            final isLoading = state is CreateCustomerLoading;

            return Scaffold(
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
                child: Form(
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
                                      .map((country) =>
                                          country['value'] as String)
                                      .toList(),
                                  onChanged: (value) => setState(() {
                                    _selectedCountry = value;
                                    _selectedCity = null;
                                    final selected = _countries.firstWhere(
                                      (c) => c['value'] == value,
                                    );

                                    // Parse country metadata for phone code
                                    _parseCountryMetadata(
                                        selected['metadata'] as String?);

                                    _cities = (selected['children'] as List)
                                        .map((city) => {
                                              'value': city['value'] as String,
                                            })
                                        .toList();
                                  }),
                                )
                              else if (_isLoadingCountries)
                                _buildLoadingDropdown(
                                    'Country (Loading...)', Icons.flag_outlined)
                              else
                                _buildTextField(
                                  controller: TextEditingController(),
                                  label: 'Country (Optional)',
                                  hint: 'Enter country name',
                                  icon: Icons.flag_outlined,
                                ),
                              const SizedBox(height: 20),
                              _buildPhoneTextField(
                                controller: _phoneController,
                                label: 'Phone Number',
                                hint: _phonePrefix.isNotEmpty
                                    ? '$_phonePrefix XXX XXXX'
                                    : 'Enter phone number',
                                icon: Icons.phone_outlined,
                                prefix: _phonePrefix.isNotEmpty
                                    ? _phonePrefix
                                    : null,
                                validator: _validatePhoneNumber,
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
                                  onChanged: (value) =>
                                      setState(() => _selectedCity = value),
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
                                  padding:
                                      const EdgeInsets.symmetric(vertical: 16),
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
                                onPressed: isLoading ? null : _submitForm,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: const Color(0xFF1B75BC),
                                  foregroundColor: Colors.white,
                                  disabledBackgroundColor:
                                      const Color(0xFF1B75BC)
                                          .withValues(alpha: 0.5),
                                  padding:
                                      const EdgeInsets.symmetric(vertical: 16),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  elevation: 0,
                                ),
                                child: isLoading
                                    ? const SizedBox(
                                        width: 20,
                                        height: 20,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          valueColor:
                                              AlwaysStoppedAnimation<Color>(
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
                ),
              ),
            );
          },
        ),
      );

  Widget _buildLoadingDropdown(String label, IconData icon) => Column(
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
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
            decoration: BoxDecoration(
              color: const Color(0xFFF9FAFB),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: Colors.grey[300]!),
            ),
            child: Row(
              children: [
                Icon(icon, color: const Color(0xFF6B7280), size: 20),
                const SizedBox(width: 12),
                const SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
                const SizedBox(width: 12),
                Text(
                  'Loading...',
                  style: TextStyle(color: Colors.grey[400], fontSize: 14),
                ),
              ],
            ),
          ),
        ],
      );

  Widget _buildPhoneTextField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    String? prefix,
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
            keyboardType: TextInputType.phone,
            inputFormatters: [
              FilteringTextInputFormatter.allow(RegExp(r'[\d\s\-\(\)\+]')),
            ],
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
              prefixText: prefix != null ? '$prefix ' : null,
              prefixStyle: const TextStyle(
                color: Color(0xFF1A1A2E),
                fontSize: 14,
                fontWeight: FontWeight.w600,
              ),
              helperText: _phoneLength > 0
                  ? 'Required length: $_phoneLength digits'
                  : null,
              helperStyle: TextStyle(
                color: Colors.grey[600],
                fontSize: 12,
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
            items: items
                .map((item) => DropdownMenuItem<String>(
                      value: item,
                      child: Text(item),
                    ))
                .toList(),
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
