import 'package:flutter/material.dart';

class InventoryPage extends StatefulWidget {
  const InventoryPage({Key? key}) : super(key: key);

  @override
  State<InventoryPage> createState() => _InventoryPageState();
}

class _InventoryPageState extends State<InventoryPage> {
  String _selectedFilter = 'All';
  final _searchController = TextEditingController();
  bool _isCreating = false;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Container(
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

  Widget _buildHeader() => Container(
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

  Widget _buildTitleRow() => Row(
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
          onPressed: () => _showAddInventoryDialog(context),
          icon: const Icon(Icons.add, size: 18),
          label: const Text(
            'New Item',
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
          ),
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFF1B75BC),
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

  Widget _buildSearchAndFilter() => Row(
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
                hintText: 'Search by name, SKU, or category...',
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
            items: ['All', 'Parts', 'Tools', 'Fluids', 'Low Stock']
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

  Widget _buildContent() => SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: _buildEmptyState(),
    );

  Widget _buildEmptyState() => Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 500),
        child: Container(
          padding: const EdgeInsets.all(48),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.08),
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
                  color: const Color(0xFF1B75BC).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(24),
                ),
                child: const Icon(
                  Icons.inventory_2_rounded,
                  size: 56,
                  color: Color(0xFF1B75BC),
                ),
              ),
              const SizedBox(height: 28),
              const Text(
                'No inventory items yet',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF1A1A2E),
                ),
              ),
              const SizedBox(height: 12),
              Text(
                'Create your first inventory item to get started.\n'
                'Inventory will sync with the backend when available.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: Colors.grey[600],
                  height: 1.6,
                ),
              ),
              const SizedBox(height: 36),
              ElevatedButton.icon(
                onPressed: () => _showAddInventoryDialog(context),
                icon: const Icon(Icons.add, size: 20),
                label: const Text(
                  'Create First Item',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF1B75BC),
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

  void _showAddInventoryDialog(BuildContext context) {
    final formKey = GlobalKey<FormState>();
    final itemNameController = TextEditingController();
    final skuController = TextEditingController();
    final quantityController = TextEditingController();
    final priceController = TextEditingController();
    final minStockController = TextEditingController();
    var selectedCategory = 'Parts';
    final categories = <String>['Parts', 'Tools', 'Fluids', 'Other'];

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
                              color: const Color(0xFF1B75BC)
                                  .withValues(alpha: 0.1),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(
                              Icons.add_box_rounded,
                              color: Color(0xFF1B75BC),
                              size: 28,
                            ),
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Text(
                              'New Inventory Item',
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

                      // Item Information
                      _buildSectionHeader('Item Information'),
                      const SizedBox(height: 14),
                      _buildTextField(
                        controller: itemNameController,
                        label: 'Item Name',
                        hint: 'Enter item name',
                        icon: Icons.label_outline_rounded,
                        isRequired: true,
                      ),
                      const SizedBox(height: 14),
                      _buildTextField(
                        controller: skuController,
                        label: 'SKU/Code',
                        hint: 'Enter SKU or part code',
                        icon: Icons.qr_code_rounded,
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
                          initialValue: selectedCategory,
                          decoration: const InputDecoration(
                            labelText: 'Category',
                            prefixIcon: Icon(
                              Icons.category_outlined,
                              color: Color(0xFF6B7280),
                            ),
                            border: InputBorder.none,
                            contentPadding: EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 14,
                            ),
                          ),
                          items: categories
                              .map((category) => DropdownMenuItem(
                                    value: category,
                                    child: Text(category),
                                  ))
                              .toList(),
                          onChanged: (value) {
                            if (value != null) {
                              setDialogState(() => selectedCategory = value);
                            }
                          },
                        ),
                      ),
                      const SizedBox(height: 20),

                      // Stock & Pricing
                      _buildSectionHeader('Stock & Pricing'),
                      const SizedBox(height: 14),
                      Row(
                        children: [
                          Expanded(
                            child: _buildTextField(
                              controller: quantityController,
                              label: 'Quantity',
                              hint: '0',
                              icon: Icons.inventory_rounded,
                              isRequired: true,
                              keyboardType: TextInputType.number,
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: _buildTextField(
                              controller: priceController,
                              label: 'Unit Price',
                              hint: '0.00',
                              icon: Icons.attach_money_rounded,
                              isRequired: true,
                              keyboardType:
                                  const TextInputType.numberWithOptions(
                                decimal: true,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      _buildTextField(
                        controller: minStockController,
                        label: 'Minimum Stock Level',
                        hint: 'Alert when stock reaches this level',
                        icon: Icons.warning_amber_rounded,
                        keyboardType: TextInputType.number,
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

                                          if (!context.mounted) {
                                            return;
                                          }

                                          setButtonState(
                                              () => _isCreating = false);
                                          setState(() => _isCreating = false);

                                          final itemName =
                                              itemNameController.text;
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
                                                          .withValues(
                                                              alpha: 0.2),
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
                                                      '$itemName added to '
                                                      'inventory successfully!',
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
                                  backgroundColor: const Color(0xFF1B75BC),
                                  foregroundColor: Colors.white,
                                  disabledBackgroundColor:
                                      const Color(0xFF1B75BC)
                                          .withValues(alpha: 0.5),
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
                                        'Add Item',
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

  Widget _buildSectionHeader(String title) => Text(
      title.toUpperCase(),
      style: const TextStyle(
        fontSize: 11,
        fontWeight: FontWeight.w700,
        color: Color(0xFF6B7280),
        letterSpacing: 1,
      ),
    );

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    bool isRequired = false,
    TextInputType? keyboardType,
  }) => TextFormField(
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
            color: Color(0xFF1B75BC),
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
              if (label == 'Quantity' || label == 'Unit Price') {
                if (label == 'Quantity' && int.tryParse(value!) == null) {
                  return 'Please enter a valid quantity';
                }
                if (label == 'Unit Price' && double.tryParse(value!) == null) {
                  return 'Please enter a valid price';
                }
              }
              return null;
            }
          : null,
    );
}
