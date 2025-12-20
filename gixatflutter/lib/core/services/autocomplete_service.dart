import 'package:graphql_flutter/graphql_flutter.dart';

import '../graphql/autocomplete_queries.dart';
import '../graphql/graphql_client.dart';
import '../models/car.dart';
import '../models/customer.dart';

class AutocompleteService {
  Future<List<Customer>> searchCustomers(String query) async {
    try {
      final client = await GraphQLConfig.getClient(withAuth: true);

      final result = await client.query(
        QueryOptions(
          document: gql(AutocompleteQueries.searchCustomers),
          variables: {
            'where': {
              'or': [
                {'firstName': {'contains': query}},
                {'lastName': {'contains': query}},
                {'phoneNumber': {'contains': query}},
              ]
            }
          },
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (result.hasException) {
        throw Exception(
            'Failed to search customers: ${result.exception.toString()}');
      }

      final List<dynamic> data = result.data?['customers'] ?? [];
      return data.map((json) => Customer.fromJson(json)).toList();
    } catch (e) {
      rethrow;
    }
  }

  Future<List<Car>> searchCars(String query) async {
    try {
      final client = await GraphQLConfig.getClient(withAuth: true);

      final result = await client.query(
        QueryOptions(
          document: gql(AutocompleteQueries.searchCars),
          variables: {
            'where': {
              'or': [
                {'licensePlate': {'contains': query}},
                {'make': {'contains': query}},
                {'model': {'contains': query}},
                {'vin': {'contains': query}},
              ]
            }
          },
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (result.hasException) {
        throw Exception(
            'Failed to search cars: ${result.exception.toString()}');
      }

      final List<dynamic> data = result.data?['cars'] ?? [];
      return data.map((json) => Car.fromJson(json)).toList();
    } catch (e) {
      rethrow;
    }
  }

  Future<List<Map<String, dynamic>>> getAutocompleteItems(
    String category, {
    String? query,
  }) async {
    try {
      final client = await GraphQLConfig.getClient(withAuth: true);

      final result = await client.query(
        QueryOptions(
          document: gql(AutocompleteQueries.getAutocompleteItems),
          variables: {
            'category': category,
            'query': query,
          },
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (result.hasException) {
        throw Exception(
            'Failed to get autocomplete items: ${result.exception.toString()}');
      }

      final List<dynamic> data = result.data?['autocompleteItems'] ?? [];
      return data.cast<Map<String, dynamic>>();
    } catch (e) {
      rethrow;
    }
  }

  Future<List<String>> getCategories() async {
    try {
      final client = await GraphQLConfig.getClient(withAuth: true);

      final result = await client.query(
        QueryOptions(
          document: gql(r'''
            query GetCategories {
              categories
            }
          '''),
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (result.hasException) {
        throw Exception(
            'Failed to get categories: ${result.exception.toString()}');
      }

      final List<dynamic> data = result.data?['categories'] ?? [];
      return data.cast<String>();
    } catch (e) {
      rethrow;
    }
  }
}
