import 'package:flutter/material.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import '../widgets/create_customer_form.dart';

class ClientsPage extends StatelessWidget {
  const ClientsPage({Key? key}) : super(key: key);

  void _showCreateCustomerForm(BuildContext context) {
    // Get the GraphQL client from the current context to pass to the new route
    final client = GraphQLProvider.of(context).value;
    
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => GraphQLProvider(
          client: client,
          child: const CreateCustomerForm(),
        ),
        fullscreenDialog: true,
      ),
    );
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: const Color(0xFF1B75BC).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: const Icon(
                  Icons.people_outline_rounded,
                  size: 64,
                  color: Color(0xFF1B75BC),
                ),
              ),
              const SizedBox(height: 24),
              const Text(
                'Clients Page',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF1A1A2E),
                ),
              ),
              const SizedBox(height: 32),
              ElevatedButton.icon(
                onPressed: () => _showCreateCustomerForm(context),
                icon: const Icon(Icons.add, size: 20),
                label: const Text(
                  'Add New Customer',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF1B75BC),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 14,
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
      );
}
