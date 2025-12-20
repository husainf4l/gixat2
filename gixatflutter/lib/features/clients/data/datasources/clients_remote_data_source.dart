import 'package:flutter/foundation.dart';
import 'package:graphql_flutter/graphql_flutter.dart';

import '../../../../core/graphql/customer_mutations.dart';
import '../../../../core/graphql/graphql_client.dart';
import '../../../../core/graphql/lookup_queries.dart';

class ClientsRemoteDataSource {
  Future<Map<String, dynamic>> createCustomer({
    required String firstName,
    required String lastName,
    required String phoneNumber,
    String? email,
    String? country,
    String? city,
    String? street,
  }) async {
    if (kDebugMode) {
      print('DataSource: Getting GraphQL client...');
    }
    
    final client = await GraphQLConfig.getClient(withAuth: true);

    if (kDebugMode) {
      print('DataSource: Executing mutation...');
      print('Variables: firstName=$firstName, lastName=$lastName, phoneNumber=$phoneNumber, email=$email, country=$country, city=$city, street=$street');
    }

    final result = await client.mutate(
      MutationOptions(
        document: gql(createCustomerMutation),
        variables: {
          'input': {
            'firstName': firstName,
            'lastName': lastName,
            'phoneNumber': phoneNumber,
            if (email != null && email.isNotEmpty) 'email': email,
            if (country != null) 'country': country,
            if (city != null) 'city': city,
            if (street != null && street.isNotEmpty) 'street': street,
          },
        },
      ),
    );

    if (kDebugMode) {
      print('DataSource: Mutation result received');
      print('  hasException: ${result.hasException}');
      print('  data: ${result.data}');
    }

    if (result.hasException) {
      if (kDebugMode) {
        print('DataSource ERROR: ${result.exception}');
      }
      throw result.exception!;
    }

    final data = result.data?['createCustomer'];
    if (data == null) {
      if (kDebugMode) {
        print('DataSource ERROR: No data returned');
      }
      throw Exception('Failed to create customer: No data returned');
    }

    if (kDebugMode) {
      print('DataSource: Customer created successfully');
    }

    return data;
  }

  Future<List<Map<String, dynamic>>> getCountries() async {
    final client = await GraphQLConfig.getClient(withAuth: true);

    final result = await client.query(
      QueryOptions(
        document: gql(getCountriesWithCitiesQuery),
        fetchPolicy: FetchPolicy.cacheFirst,
      ),
    );

    if (result.hasException) {
      throw result.exception!;
    }

    final items = result.data?['lookupItems'] as List?;
    if (items == null) {
      return [];
    }

    return items
        .map((item) => {
              'value': item['value'] as String,
              'metadata': item['metadata'] as String?,
              'children': item['children'] as List,
            })
        .toList();
  }
}
