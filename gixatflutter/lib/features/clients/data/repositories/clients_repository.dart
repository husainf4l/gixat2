import 'package:flutter/foundation.dart';
import 'package:graphql_flutter/graphql_flutter.dart';

import '../datasources/clients_remote_data_source.dart';

class ClientsRepository {
  ClientsRepository({
    ClientsRemoteDataSource? remoteDataSource,
  }) : _remoteDataSource = remoteDataSource ?? ClientsRemoteDataSource();

  final ClientsRemoteDataSource _remoteDataSource;

  Future<String> createCustomer({
    required String firstName,
    required String lastName,
    required String phoneNumber,
    String? email,
    String? country,
    String? city,
    String? street,
  }) async {
    try {
      if (kDebugMode) {
        print('Repository: Creating customer...');
      }
      
      final customerData = await _remoteDataSource.createCustomer(
        firstName: firstName,
        lastName: lastName,
        phoneNumber: phoneNumber,
        email: email,
        country: country,
        city: city,
        street: street,
      );
      
      if (kDebugMode) {
        print('Repository: Customer created successfully');
      }
      
      return customerData['id'] as String;
    } catch (e, stackTrace) {
      if (kDebugMode) {
        print('Repository ERROR: $e');
        print('Stack trace: $stackTrace');
      }
      
      if (e is OperationException) {
        throw _handleGraphQLException(e);
      }
      throw Exception('Failed to create customer: ${e.toString()}');
    }
  }

  Future<List<Map<String, dynamic>>> getCountries() async {
    try {
      return await _remoteDataSource.getCountries();
    } catch (e) {
      if (e is OperationException) {
        throw _handleGraphQLException(e);
      }
      // Return empty list on error for lookup data to not block the UI
      return [];
    }
  }

  Exception _handleGraphQLException(OperationException exception) {
    if (kDebugMode) {
      print('GraphQL Exception Details:');
      print('  graphqlErrors: ${exception.graphqlErrors}');
      print('  linkException: ${exception.linkException}');
    }
    
    if (exception.graphqlErrors.isNotEmpty) {
      final message = exception.graphqlErrors.first.message;
      if (kDebugMode) {
        print('  GraphQL error message: $message');
      }
      return Exception(message);
    }

    if (exception.linkException != null) {
      if (kDebugMode) {
        print('  Network/Link error: ${exception.linkException}');
      }
      return Exception('Network error. Please check your connection.');
    }

    return Exception('An error occurred. Please try again.');
  }
}
