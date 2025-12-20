import 'package:graphql_flutter/graphql_flutter.dart';
import '../models/user_model.dart';
import '../../../../core/graphql/graphql_client.dart';
import '../../../../core/graphql/auth_queries.dart';
import '../../../../core/storage/secure_storage_service.dart';

class AuthRepository {
  final SecureStorageService _storage;

  AuthRepository({
    required SecureStorageService storage,
  }) : _storage = storage;

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
    required String garageName,
    required String ownerName,
    required String email,
    required String password,
  }) async {
    try {
      print('DEBUG: Starting registration for $email');
      final client = await GraphQLConfig.getClient(withAuth: false);
      print('DEBUG: GraphQL client obtained');
      
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

      print('DEBUG: Mutation executed');
      
      if (result.hasException) {
        print('DEBUG: Has exception: ${result.exception}');
        throw _handleGraphQLException(result.exception!);
      }

      print('DEBUG: Result data: ${result.data}');
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

      print('DEBUG: Registration successful, token: ${token?.substring(0, 20)}...');
      return AuthResponse(token: token, user: user);
    } catch (e) {
      print('DEBUG: Registration error: $e');
      throw Exception('Registration failed: ${e.toString()}');
    }
  }

  /// Check if token is valid using GraphQL
  Future<bool> isTokenValid() async {
    try {
      final token = await _storage.getToken();
      if (token == null) return false;

      final client = await GraphQLConfig.getClient(withAuth: true);
      
      final result = await client.query(
        QueryOptions(
          document: gql(meQuery),
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      return !result.hasException && result.data?['me'] != null;
    } catch (e) {
      return false;
    }
  }

  /// Logout and clear stored data
  Future<void> logout() async {
    await _storage.clearAll();
  }

  /// Get stored token
  Future<String?> getStoredToken() async {
    return await _storage.getToken();
  }

  /// Handle GraphQL exceptions to user-friendly errors
  Exception _handleGraphQLException(OperationException exception) {
    print('DEBUG: Exception details - linkException: ${exception.linkException}, graphqlErrors: ${exception.graphqlErrors}');
    
    final errors = exception.graphqlErrors;
    if (errors.isNotEmpty) {
      final message = errors.first.message;
      print('DEBUG: GraphQL error message: $message');
      
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
      print('DEBUG: Link exception: ${exception.linkException}');
      return Exception('Network error. Please check your connection.');
    }

    return Exception('An error occurred. Please try again.');
  }
}
