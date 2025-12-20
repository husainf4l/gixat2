import 'package:flutter/material.dart';

class InvoicesPage extends StatefulWidget {
  const InvoicesPage({Key? key}) : super(key: key);

  @override
  State<InvoicesPage> createState() => _InvoicesPageState();
}

class _InvoicesPageState extends State<InvoicesPage> {
  String _selectedFilter = 'All';
  final _searchController = TextEditingController();
  bool _isCreating = false;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFFF5F7FA),
      child: Column(
        children: [
          _buildHeader(),
          Expanded(
            child: _buildContent(),
          ),
        ],
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.fromLTRB(24, 24, 24, 20),
      child: SafeArea(
        bottom: false,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildTitleRow(),
            const SizedBox(height: 24),
            _buildSearchAndFilter(),
          ],
        ),
      ),
    );
  }

  Widget _buildTitleRow() {
    return Row(
      children: [
        Container(
          decoration: BoxDecoration(
            color: const Color(0xFFF5F7FA),
            borderRadius: BorderRadius.circular(12),
          ),
          child: IconButton(
            icon: const Icon(Icons.menu_rounded, size: 24),
            onPressed: () => Scaffold.of(context).openDrawer(),
            color: const Color(0xFF1A1A2E),
          ),
        ),
        const Spacer(),
        ElevatedButton.icon(
          onPressed: () => _showAddInvoiceDialog(context),
          icon: const Icon(Icons.add, size: 18),
          label: const Text(
            'New Invoice',
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
          ),
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFF6366F1),
            foregroundColor: Colors.white,
            padding: const EdgeInsets.symmetric(
              horizontal: 20,
              vertical: 14,
            ),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            elevation: 0,
          ),
        ),
      ],
    );
  }

  Widget _buildSearchAndFilter() {
    return Row(
      children: [
        Expanded(
          child: Container(
            decoration: BoxDecoration(
              color: const Color(0xFFF5F7FA),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: const Color(0xFFE5E7EB),
                width: 1,
              ),
            ),
            child: TextField(
              controller: _searchController,
              decoration: const InputDecoration(
                hintText: 'Search by client, invoice number...',
                hintStyle: TextStyle(
                  color: Color(0xFF9CA3AF),
                  fontSize: 14,
                ),
                prefixIcon: Icon(
                  Icons.search_rounded,
                  color: Color(0xFF6B7280),
                  size: 20,
                ),
                border: InputBorder.none,
                contentPadding: EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 14,
                ),
              ),
              onChanged: (value) {
                // TODO: Implement search functionality
              },
            ),
          ),
        ),
        const SizedBox(width: 12),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          decoration: BoxDecoration(
            color: const Color(0xFFF5F7FA),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: const Color(0xFFE5E7EB),
              width: 1,
            ),
          ),
          child: DropdownButton<String>(
            value: _selectedFilter,
            underline: const SizedBox(),
            icon: const Icon(Icons.filter_list_rounded, size: 20),
            style: const TextStyle(
              color: Color(0xFF1A1A2E),
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
            items: ['All', 'Draft', 'Sent', 'Paid', 'Overdue']
                .map((filter) => DropdownMenuItem(
                      value: filter,
                      child: Text(filter),
                    ))
                .toList(),
            onChanged: (value) {
              if (value != null) {
                setState(() => _selectedFilter = value);
              }
            },
          ),
        ),
      ],
    );
  }

  Widget _buildContent() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: _buildEmptyState(),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 500),
        child: Container(
          padding: const EdgeInsets.all(48),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.08),
                blurRadius: 20,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: const Color(0xFF6366F1).withOpacity(0.1),
                  borderRadius: BorderRadius.circular(24),
                ),
                child: const Icon(
                  Icons.receipt_rounded,
                  size: 56,
                  color: Color(0xFF6366F1),
                ),
              ),
              const SizedBox(height: 28),
              const Text(
                'No invoices yet',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF1A1A2E),
                ),
              ),
              const SizedBox(height: 12),
              Text(
                'Create your first invoice to get started.\nInvoices will sync with the backend when available.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: Colors.grey[600],
                  height: 1.6,
                ),
              ),
              const SizedBox(height: 36),
              ElevatedButton.icon(
                onPressed: () => _showAddInvoiceDialog(context),
                icon: const Icon(Icons.add, size: 20),
                label: const Text(
                  'Create First Invoice',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF6366F1),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 28,
                    vertical: 16,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  elevation: 0,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showAddInvoiceDialog(BuildContext context) {
    final formKey = GlobalKey<FormState>();
    final clientController = TextEditingController();
    final amountController = TextEditingController();
    final itemsController = TextEditingController();
    DateTime? selectedDueDate = DateTime.now().add(const Duration(days: 30));
    String selectedStatus = 'Draft';
    final invoiceNumber = 'INV-${DateTime.now().millisecondsSinceEpoch.toString().substring(7)}';
    final List<String> statuses = ['Draft', 'Sent', 'Paid'];

    showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => Dialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          insetPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 24,
          ),
          child: Container(
            width: double.infinity,
            child: SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Form(
                  key: formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Dialog Header
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: const Color(0xFF6366F1).withOpacity(0.1),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(
                              Icons.receipt_long_rounded,
                              color: Color(0xFF6366F1),
                              size: 28,
                            ),
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Text(
                              'New Invoice',
                              style: TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.w700,
                                color: Color(0xFF1A1A2E),
                              ),
                            ),
                          ),
                          IconButton(
                            onPressed: () => Navigator.pop(context),
                            icon: const Icon(Icons.close),
                            color: const Color(0xFF6B7280),
                          ),
                        ],
                      ),
                      const SizedBox(height: 28),

                      // Invoice Number (Auto-generated)
                      _buildSectionHeader('Invoice Number'),
                      const SizedBox(height: 14),
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: const Color(0xFFF9FAFB),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                            color: const Color(0xFFE5E7EB),
                          ),
                        ),
                        child: Row(
                          children: [
                            const Icon(
                              Icons.tag_rounded,
                              color: Color(0xFF6B7280),
                              size: 20,
                            ),
                            const SizedBox(width: 12),
                            Text(
                              invoiceNumber,
                              style: const TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w600,
                                color: Color(0xFF1A1A2E),
                              ),
                            ),
                            const Spacer(),
                            Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 8,
                                vertical: 4,
                              ),
                              decoration: BoxDecoration(
                                color: const Color(0xFF10B981).withOpacity(0.1),
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: const Text(
                                'Auto-generated',
                                style: TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                  color: Color(0xFF10B981),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 20),

                      // Client Information
                      _buildSectionHeader('Client Information'),
                      const SizedBox(height: 14),
                      _buildTextField(
                        controller: clientController,
                        label: 'Client Name',
                        hint: 'Enter client full name',
                        icon: Icons.person_outline_rounded,
                        isRequired: true,
                      ),
                      const SizedBox(height: 20),

                      // Invoice Details
                      _buildSectionHeader('Invoice Details'),
                      const SizedBox(height: 14),
                      _buildTextField(
                        controller: amountController,
                        label: 'Amount',
                        hint: '0.00',
                        icon: Icons.attach_money_rounded,
                        isRequired: true,
                        keyboardType: const TextInputType.numberWithOptions(decimal: true),
                      ),
                      const SizedBox(height: 14),
                      OutlinedButton.icon(
                        onPressed: () async {
                          final date = await showDatePicker(
                            context: context,
                            initialDate: selectedDueDate ?? DateTime.now(),
                            firstDate: DateTime.now(),
                            lastDate: DateTime.now().add(const Duration(days: 365)),
                          );
                          if (date != null) {
                            setDialogState(() => selectedDueDate = date);
                          }
                        },
                        icon: const Icon(Icons.calendar_today_rounded, size: 18),
                        label: Text(
                          selectedDueDate != null
                              ? 'Due: ${selectedDueDate!.day}/${selectedDueDate!.month}/${selectedDueDate!.year}'
                              : 'Select Due Date',
                          style: const TextStyle(fontSize: 14),
                        ),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: const Color(0xFF1A1A2E),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 18,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                          side: const BorderSide(color: Color(0xFFE5E7EB)),
                          backgroundColor: const Color(0xFFF9FAFB),
                        ),
                      ),
                      const SizedBox(height: 14),
                      Container(
                        decoration: BoxDecoration(
                          color: const Color(0xFFF9FAFB),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                            color: const Color(0xFFE5E7EB),
                          ),
                        ),
                        child: DropdownButtonFormField<String>(
                          value: selectedStatus,
                          decoration: const InputDecoration(
                            labelText: 'Status',
                            prefixIcon: Icon(
                              Icons.circle_rounded,
                              color: Color(0xFF6B7280),
                            ),
                            border: InputBorder.none,
                            contentPadding: EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 14,
                            ),
                          ),
                          items: statuses
                              .map((status) => DropdownMenuItem(
                                    value: status,
                                    child: Text(status),
                                  ))
                              .toList(),
                          onChanged: (value) {
                            if (value != null) {
                              setDialogState(() => selectedStatus = value);
                            }
                          },
                        ),
                      ),
                      const SizedBox(height: 20),

                      // Items
                      _buildSectionHeader('Items (Optional)'),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: itemsController,
                        decoration: InputDecoration(
                          hintText: 'List items and services...',
                          hintStyle: const TextStyle(
                            color: Color(0xFF9CA3AF),
                            fontSize: 14,
                          ),
                          prefixIcon: const Icon(
                            Icons.list_alt_rounded,
                            color: Color(0xFF6B7280),
                          ),
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: Color(0xFFE5E7EB),
                            ),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: Color(0xFFE5E7EB),
                            ),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: Color(0xFF6366F1),
                              width: 2,
                            ),
                          ),
                          filled: true,
                          fillColor: const Color(0xFFF9FAFB),
                          contentPadding: const EdgeInsets.all(16),
                        ),
                        maxLines: 4,
                      ),
                      const SizedBox(height: 32),

                      // Action Buttons
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(
                            child: TextButton(
                              onPressed: () => Navigator.pop(context),
                              child: const Text(
                                'Cancel',
                                style: TextStyle(
                                  color: Color(0xFF6B7280),
                                  fontSize: 15,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            flex: 2,
                            child: StatefulBuilder(
                              builder: (context, setButtonState) =>
                                  ElevatedButton(
                                onPressed: _isCreating
                                    ? null
                                    : () async {
                                        if (formKey.currentState!.validate()) {
                                          setButtonState(
                                              () => _isCreating = true);
                                          setState(() => _isCreating = true);

                                          // Simulate backend call
                                          await Future.delayed(
                                              const Duration(seconds: 1));

                                          if (!context.mounted) return;

                                          setButtonState(
                                              () => _isCreating = false);
                                          setState(() => _isCreating = false);

                                          ScaffoldMessenger.of(context)
                                              .showSnackBar(
                                            SnackBar(
                                              content: Row(
                                                children: [
                                                  Container(
                                                    padding:
                                                        const EdgeInsets.all(6),
                                                    decoration: BoxDecoration(
                                                      color: Colors.white
                                                          .withOpacity(0.2),
                                                      borderRadius:
                                                          BorderRadius.circular(
                                                              8),
                                                    ),
                                                    child: const Icon(
                                                      Icons
                                                          .check_circle_outline,
                                                      color: Colors.white,
                                                      size: 20,
                                                    ),
                                                  ),
                                                  const SizedBox(width: 12),
                                                  Expanded(
                                                    child: Text(
                                                      'Invoice $invoiceNumber created successfully!',
                                                      style: const TextStyle(
                                                          fontSize: 14),
                                                    ),
                                                  ),
                                                ],
                                              ),
                                              backgroundColor:
                                                  const Color(0xFF10B981),
                                              behavior:
                                                  SnackBarBehavior.floating,
                                              shape: RoundedRectangleBorder(
                                                borderRadius:
                                                    BorderRadius.circular(12),
                                              ),
                                              margin: const EdgeInsets.all(16),
                                              duration:
                                                  const Duration(seconds: 3),
                                            ),
                                          );
                                          Navigator.pop(context);
                                        }
                                      },
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: const Color(0xFF6366F1),
                                  foregroundColor: Colors.white,
                                  disabledBackgroundColor:
                                      const Color(0xFF6366F1).withOpacity(0.5),
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 32,
                                    vertical: 16,
                                  ),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  elevation: 0,
                                ),
                                child: _isCreating
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
                                        'Create Invoice',
                                        style: TextStyle(
                                          fontSize: 15,
                                          fontWeight: FontWeight.w600,
                                        ),
                                      ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSectionHeader(String title) {
    return Text(
      title.toUpperCase(),
      style: const TextStyle(
        fontSize: 11,
        fontWeight: FontWeight.w700,
        color: Color(0xFF6B7280),
        letterSpacing: 1,
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    bool isRequired = false,
    TextInputType? keyboardType,
  }) {
    return TextFormField(
      controller: controller,
      keyboardType: keyboardType,
      decoration: InputDecoration(
        labelText: isRequired ? '$label *' : label,
        hintText: hint,
        hintStyle: const TextStyle(
          color: Color(0xFF9CA3AF),
          fontSize: 14,
        ),
        prefixIcon: Icon(
          icon,
          color: const Color(0xFF6B7280),
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFFE5E7EB)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFFE5E7EB)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(
            color: Color(0xFF6366F1),
            width: 2,
          ),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: Color(0xFFEF4444)),
        ),
        filled: true,
        fillColor: const Color(0xFFF9FAFB),
        contentPadding: const EdgeInsets.all(16),
      ),
      validator: isRequired
          ? (value) {
              if (value?.isEmpty ?? true) {
                return '$label is required';
              }
              if (label == 'Amount' && double.tryParse(value!) == null) {
                return 'Please enter a valid amount';
              }
              return null;
            }
          : null,
    );
  }
}
