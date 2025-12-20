import 'package:flutter/foundation.dart';
import 'package:graphql_flutter/graphql_flutter.dart';

import '../../../../core/graphql/auth_queries.dart';
import '../../../../core/graphql/graphql_client.dart';

class AuthRemoteDataSource {
  Future<Map<String, dynamic>> login({
    required String email,
    required String password,
  }) async {
    final client = await GraphQLConfig.getClient(withAuth: false);

    final result = await client.mutate(
      MutationOptions(
        document: gql(loginMutation),
        variables: {
          'email': email,
          'password': password,
        },
      ),
    );

    if (result.hasException) {
      throw result.exception!;
    }

    final data = result.data?['login'];
    if (data == null) {
      throw Exception('Login failed: No data returned');
    }

    if (data['error'] != null) {
      throw Exception(data['error']);
    }

    return data;
  }

  Future<Map<String, dynamic>> register({
    required String ownerName,
    required String email,
    required String password,
  }) async {
    final client = await GraphQLConfig.getClient(withAuth: false);

    final result = await client.mutate(
      MutationOptions(
        document: gql(registerMutation),
        variables: {
          'email': email,
          'password': password,
          'fullName': ownerName,
          'role': 'OWNER',
          'userType': 'ORGANIZATIONAL',
        },
      ),
    );

    if (result.hasException) {
      throw result.exception!;
    }

    final data = result.data?['register'];
    if (data == null) {
      throw Exception('Registration failed: No data returned');
    }

    if (data['error'] != null) {
      throw Exception(data['error']);
    }

    return data;
  }

  Future<bool> isTokenValid() async {
    final client = await GraphQLConfig.getClient(withAuth: true);

    final result = await client.query(
      QueryOptions(
        document: gql(meQuery),
        fetchPolicy: FetchPolicy.networkOnly,
      ),
    );

    return !result.hasException && result.data?['me'] != null;
  }

  Future<Map<String, dynamic>> getCurrentUser() async {
    final client = await GraphQLConfig.getClient(withAuth: true);

    final result = await client.query(
      QueryOptions(
        document: gql(meQuery),
        fetchPolicy: FetchPolicy.networkOnly,
      ),
    );

    if (result.hasException) {
      throw result.exception!;
    }

    final data = result.data?['me'];
    if (data == null) {
      throw Exception('User not found');
    }

    if (kDebugMode) {
      print('=== ME QUERY RESPONSE ===');
      print('Full data: $data');
      print('organizationId: ${data['organizationId']}');
      print('organization: ${data['organization']}');
    }

    // Flatten organization data for easier access
    final Map<String, dynamic> userData = Map<String, dynamic>.from(data);
    
    // Try to get organization name from nested query first
    if (userData['organization'] != null && userData['organization'] is Map) {
      final org = userData['organization'] as Map<String, dynamic>;
      userData['organizationName'] = org['name'] as String?;
      
      if (kDebugMode) {
        print('Extracted organizationName from nested query: ${userData['organizationName']}');
      }
    } else if (userData['organizationId'] != null) {
      // If nested organization is null but organizationId exists, fetch separately
      if (kDebugMode) {
        print('Organization nested field is null, fetching with myOrganization query');
      }
      
      try {
        final orgResult = await client.query(
          QueryOptions(
            document: gql(myOrganizationQuery),
            fetchPolicy: FetchPolicy.networkOnly,
          ),
        );
        
        if (kDebugMode) {
          print('myOrganization query completed');
          print('Has exception: ${orgResult.hasException}');
          print('Data: ${orgResult.data}');
          if (orgResult.hasException) {
            print('Exception: ${orgResult.exception}');
          }
        }
        
        if (!orgResult.hasException && orgResult.data?['myOrganization'] != null) {
          final org = orgResult.data!['myOrganization'] as Map<String, dynamic>;
          userData['organizationName'] = org['name'] as String?;
          
          if (kDebugMode) {
            print('Fetched organizationName from myOrganization: ${userData['organizationName']}');
          }
        } else {
          if (kDebugMode) {
            print('myOrganization query returned null or had exception');
          }
        }
      } catch (e) {
        if (kDebugMode) {
          print('Failed to fetch myOrganization: $e');
        }
      }
    } else {
      if (kDebugMode) {
        print('WARNING: No organizationId found');
      }
    }

    return userData;
  }

  Future<Map<String, dynamic>> createOrganization({
    required String name,
    required String country,
    required String city,
    required String street,
    required String phoneCountryCode,
  }) async {
    final client = await GraphQLConfig.getClient(withAuth: true);

    final result = await client.mutate(
      MutationOptions(
        document: gql(createOrganizationMutation),
        variables: {
          'input': {
            'name': name,
            'country': country,
            'city': city,
            'street': street,
            'phoneCountryCode': phoneCountryCode,
          },
        },
      ),
    );

    if (result.hasException) {
      throw result.exception!;
    }

    final data = result.data?['createOrganization'];
    if (data == null) {
      throw Exception('Failed to create organization');
    }

    return data;
  }
}
