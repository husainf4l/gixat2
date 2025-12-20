import 'package:graphql_flutter/graphql_flutter.dart';

import '../../../../core/graphql/auth_queries.dart';
import '../../../../core/graphql/graphql_client.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../models/user_model.dart';

class AuthRepository {
  AuthRepository({
    required SecureStorageService storage,
  }) : _storage = storage;
  final SecureStorageService _storage;

  /// Login with email and password using GraphQL
  Future<AuthResponse> login({
    required String email,
    required String password,
  }) async {
    try {
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
        throw _handleGraphQLException(result.exception!);
      }

      final data = result.data?['login'];
      if (data == null) {
        throw Exception('Login failed: No data returned');
      }

      if (data['error'] != null) {
        throw Exception(data['error']);
      }

      final token = data['token'] as String?;
      if (token == null) {
        throw Exception('Login failed: No token returned');
      }

      final userData = data['user'] as Map<String, dynamic>;

      // Save token and user info
      await _storage.saveToken(token);
      await _storage.saveUserId(userData['id'].toString());
      await _storage.saveUserEmail(userData['email'] as String);
      if (userData['fullName'] != null) {
        await _storage.saveUserName(userData['fullName'] as String);
      }

      final user = User(
        id: userData['id'].toString(),
        email: userData['email'] as String,
        role: userData['role'] as String? ?? 'owner',
      );

      return AuthResponse(token: token, user: user);
    } catch (e) {
      throw Exception('Login failed: ${e.toString()}');
    }
  }

  /// Register new account using GraphQL
  Future<AuthResponse> register({
    required String ownerName,
    required String email,
    required String password,
  }) async {
    try {
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
        throw _handleGraphQLException(result.exception!);
      }

      final data = result.data?['register'];
      if (data == null) {
        throw Exception('Registration failed: No data returned');
      }

      if (data['error'] != null) {
        throw Exception(data['error']);
      }

      final token = data['token'] as String?;
      if (token == null) {
        throw Exception('Registration failed: No token returned');
      }

      final userData = data['user'] as Map<String, dynamic>;

      // Auto-save token after registration
      await _storage.saveToken(token);
      await _storage.saveUserId(userData['id'].toString());
      await _storage.saveUserEmail(userData['email'] as String);
      if (userData['fullName'] != null) {
        await _storage.saveUserName(userData['fullName'] as String);
      }

      final user = User(
        id: userData['id'].toString(),
        email: userData['email'] as String,
        role: userData['role'] as String? ?? 'owner',
      );

      return AuthResponse(token: token, user: user);
    } catch (e) {
      throw Exception('Registration failed: ${e.toString()}');
    }
  }

  /// Check if token is valid using GraphQL
  Future<bool> isTokenValid() async {
    try {
      final token = await _storage.getToken();
      if (token == null) {
        return false;
      }

      final client = await GraphQLConfig.getClient(withAuth: true);

      final result = await client.query(
        QueryOptions(
          document: gql(meQuery),
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      return !result.hasException && result.data?['me'] != null;
    } on Exception {
      return false;
    }
  }

  Future<bool> hasGarage() async {
    final garageId = await _storage.getGarageId();
    return garageId != null && garageId.isNotEmpty;
  }

  Future<void> saveGarageId(String garageId) async {
    await _storage.saveGarageId(garageId);
  }

  /// Create a new organization (garage)
  Future<String> createOrganization({
    required String name,
    required String country,
    required String city,
    required String street,
    required String phoneCountryCode,
  }) async {
    try {
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
        throw _handleGraphQLException(result.exception!);
      }

      final data = result.data?['createOrganization'];
      if (data == null) {
        throw Exception('Failed to create organization');
      }

      final garageId = data['id'].toString();
      await saveGarageId(garageId);
      return garageId;
    } catch (e) {
      throw Exception('Failed to create organization: ${e.toString()}');
    }
  }

  /// Logout and clear stored data
  Future<void> logout() async {
    await _storage.clearAll();
  }

  /// Get stored token
  Future<String?> getStoredToken() async => _storage.getToken();

  /// Handle GraphQL exceptions to user-friendly errors
  Exception _handleGraphQLException(OperationException exception) {
    final errors = exception.graphqlErrors;
    if (errors.isNotEmpty) {
      final message = errors.first.message;

      if (message.toLowerCase().contains('credentials') ||
          message.toLowerCase().contains('password')) {
        return Exception('Invalid email or password.');
      } else if (message.toLowerCase().contains('exists') ||
          message.toLowerCase().contains('already')) {
        return Exception('Email already registered.');
      }

      return Exception(message);
    }

    if (exception.linkException != null) {
      return Exception('Network error. Please check your connection.');
    }

    return Exception('An error occurred. Please try again.');
  }
}
