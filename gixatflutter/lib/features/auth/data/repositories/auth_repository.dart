import 'package:graphql_flutter/graphql_flutter.dart';

import '../../../../core/storage/secure_storage_service.dart';
import '../datasources/auth_remote_data_source.dart';
import '../models/user_model.dart';

class AuthRepository {
  AuthRepository({
    required SecureStorageService storage,
    AuthRemoteDataSource? authRemoteDataSource,
  })  : _storage = storage,
        _authRemoteDataSource = authRemoteDataSource ?? AuthRemoteDataSource();

  final SecureStorageService _storage;
  final AuthRemoteDataSource _authRemoteDataSource;

  /// Login with email and password using GraphQL
  Future<AuthResponse> login({
    required String email,
    required String password,
  }) async {
    try {
      final data = await _authRemoteDataSource.login(
        email: email,
        password: password,
      );

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
      if (e is OperationException) {
        throw _handleGraphQLException(e);
      }
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
      final data = await _authRemoteDataSource.register(
        ownerName: ownerName,
        email: email,
        password: password,
      );

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
      if (e is OperationException) {
        throw _handleGraphQLException(e);
      }
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

      return await _authRemoteDataSource.isTokenValid();
    } on Exception {
      return false;
    }
  }

  /// Get current user info including organizationId
  Future<UserInfo?> getCurrentUser() async {
    try {
      final token = await _storage.getToken();
      if (token == null) {
        return null;
      }

      final data = await _authRemoteDataSource.getCurrentUser();
      return UserInfo(
        id: data['id'].toString(),
        email: data['email'] as String,
        fullName: data['fullName'] as String?,
        organizationId: data['organizationId'] as String?,
        organizationName: data['organizationName'] as String?,
      );
    } catch (e) {
      if (e is OperationException) {
        throw _handleGraphQLException(e);
      }
      return null;
    }
  }

  Future<bool> hasGarage() async {
    // Check if user has an organization from the server
    final userInfo = await getCurrentUser();
    if (userInfo?.organizationId != null && userInfo!.organizationId!.isNotEmpty) {
      // Save the organization ID to local storage for future use
      await saveGarageId(userInfo.organizationId!);
      return true;
    }
    return false;
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
      final data = await _authRemoteDataSource.createOrganization(
        name: name,
        country: country,
        city: city,
        street: street,
        phoneCountryCode: phoneCountryCode,
      );

      final garageId = data['id'].toString();
      await saveGarageId(garageId);
      return garageId;
    } catch (e) {
      if (e is OperationException) {
        throw _handleGraphQLException(e);
      }
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
