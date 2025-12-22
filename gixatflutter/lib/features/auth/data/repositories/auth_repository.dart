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
      print('📡 Attempting login for: $email');
      final data = await _authRemoteDataSource.login(
        email: email,
        password: password,
      );

      print('📦 Login response data: $data');
      
      // Check for error first
      if (data['error'] != null) {
        final errorMsg = data['error'].toString();
        print('❌ Server returned error: $errorMsg');
        throw Exception(errorMsg);
      }

      final token = data['token'];
      if (token == null || token.toString().isEmpty) {
        print('❌ No token in response');
        throw Exception('Login failed: No token returned');
      }

      final userData = data['user'];
      if (userData == null) {
        print('❌ No user data in response');
        throw Exception('Login failed: No user data returned');
      }
      
      if (userData is! Map<String, dynamic>) {
        print('❌ User data is not a map: ${userData.runtimeType}');
        throw Exception('Login failed: Invalid user data format');
      }

      // Validate required user fields
      final userId = userData['id'];
      final userEmail = userData['email'];
      
      if (userId == null || userId.toString().isEmpty) {
        print('❌ No user ID in response');
        throw Exception('Login failed: No user ID returned');
      }
      if (userEmail == null || userEmail.toString().isEmpty) {
        print('❌ No user email in response');
        throw Exception('Login failed: No email returned');
      }

      // Save token and user info
      await _storage.saveToken(token.toString());
      await _storage.saveUserId(userId.toString());
      await _storage.saveUserEmail(userEmail.toString());
      if (userData['fullName'] != null) {
        await _storage.saveUserName(userData['fullName'].toString());
      }

      final user = User(
        id: userId.toString(),
        email: userEmail.toString(),
        role: userData['role']?.toString() ?? 'owner',
      );

      print('✅ Login successful for: ${user.email}');
      return AuthResponse(token: token, user: user);
    } catch (e) {
      print('❌ Login error: $e');
      print('❌ Error type: ${e.runtimeType}');
      if (e is OperationException) {
        print('❌ GraphQL Errors: ${e.graphqlErrors}');
        print('❌ Link Exception: ${e.linkException}');
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
    String? organizationId,
  }) async {
    try {
      print('📡 Attempting registration for: $email with orgId: $organizationId');
      final data = await _authRemoteDataSource.register(
        ownerName: ownerName,
        email: email,
        password: password,
        organizationId: organizationId,
      );

      print('📦 Registration response data: $data');

      // Check for error first
      if (data['error'] != null) {
        final errorMsg = data['error'].toString();
        print('❌ Server returned error: $errorMsg');
        throw Exception(errorMsg);
      }

      final token = data['token'];
      if (token == null || token.toString().isEmpty) {
        print('❌ No token in response');
        throw Exception('Registration failed: No token returned');
      }

      final userData = data['user'];
      if (userData == null) {
        print('❌ No user data in response');
        throw Exception('Registration failed: No user data returned');
      }
      
      if (userData is! Map<String, dynamic>) {
        print('❌ User data is not a map: ${userData.runtimeType}');
        throw Exception('Registration failed: Invalid user data format');
      }

      print('👤 User data: $userData');

      // Validate required user fields
      final userId = userData['id'];
      final userEmail = userData['email'];
      
      if (userId == null || userId.toString().isEmpty) {
        print('❌ No user ID in response');
        throw Exception('Registration failed: No user ID returned');
      }
      if (userEmail == null || userEmail.toString().isEmpty) {
        print('❌ No user email in response');
        throw Exception('Registration failed: No email returned');
      }

      // Auto-save token after registration
      await _storage.saveToken(token.toString());
      await _storage.saveUserId(userId.toString());
      await _storage.saveUserEmail(userEmail.toString());
      if (userData['fullName'] != null) {
        await _storage.saveUserName(userData['fullName'].toString());
      }

      final user = User(
        id: userId.toString(),
        email: userEmail.toString(),
        role: 'owner', // Default role, not returned in register response
      );

      print('✅ Registration successful for: ${user.email}');
      return AuthResponse(token: token, user: user);
    } catch (e) {
      print('❌ Registration error: $e');
      print('❌ Error type: ${e.runtimeType}');
      if (e is OperationException) {
        print('❌ GraphQL Errors: ${e.graphqlErrors}');
        print('❌ Link Exception: ${e.linkException}');
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

  /// Create a new organization (garage) - can be used for signup or after auth
  Future<AuthResponse?> createOrganization({
    required String name,
    required String country,
    required String city,
    required String street,
    required String phoneCountryCode,
    String? email,
    String? password,
    String? fullName,
  }) async {
    try {
      print('📡 Creating organization: $name');
      final data = await _authRemoteDataSource.createOrganization(
        name: name,
        country: country,
        city: city,
        street: street,
        phoneCountryCode: phoneCountryCode,
        email: email,
        password: password,
        fullName: fullName,
      );

      print('📦 CreateOrg response data: $data');

      // Check if this is an auth response (signup flow) or just organization data
      if (data['token'] != null && data['user'] != null) {
        // This is signup flow - handle like register
        final token = data['token'];
        if (token == null || token.toString().isEmpty) {
          throw Exception('Organization creation failed: No token returned');
        }

        final userData = data['user'];
        if (userData == null || userData is! Map<String, dynamic>) {
          throw Exception('Organization creation failed: Invalid user data');
        }

        final userId = userData['id'];
        final userEmail = userData['email'];
        
        if (userId == null || userId.toString().isEmpty) {
          throw Exception('Organization creation failed: No user ID returned');
        }
        if (userEmail == null || userEmail.toString().isEmpty) {
          throw Exception('Organization creation failed: No email returned');
        }

        // Save token and user info
        await _storage.saveToken(token.toString());
        await _storage.saveUserId(userId.toString());
        await _storage.saveUserEmail(userEmail.toString());
        if (userData['fullName'] != null) {
          await _storage.saveUserName(userData['fullName'].toString());
        }

        final user = User(
          id: userId.toString(),
          email: userEmail.toString(),
          role: 'owner',
        );

        print('✅ Organization created and user registered: ${user.email}');
        return AuthResponse(token: token.toString(), user: user);
      } else {
        // This is post-auth organization creation
        final garageId = data['id']?.toString();
        if (garageId != null) {
          await saveGarageId(garageId);
        }
        return null;
      }
    } catch (e) {
      print('❌ Create organization error: $e');
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
