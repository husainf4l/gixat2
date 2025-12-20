// Example: How to test the auth module locally

// OPTION 1: Mock API Response (for UI testing)
// ============================================
// Use this to test UI without a backend

import 'package:mockito/mockito.dart';
import 'package:dio/dio.dart';

class MockNetworkClient extends Mock implements NetworkClient {}
class MockSecureStorageService extends Mock implements SecureStorageService {}

void mockSuccessfulLogin() {
  final mockNetworkClient = MockNetworkClient();
  
  when(mockNetworkClient.post<Map<String, dynamic>>(
    '/auth/login',
    data: anyNamed('data'),
  )).thenAnswer((_) async => Response(
    requestOptions: RequestOptions(path: '/auth/login'),
    statusCode: 200,
    data: {
      'token': 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
      'user': {
        'id': 'user_123',
        'email': 'user@example.com',
        'role': 'owner',
      },
    },
  ));
}

// OPTION 2: JSON Server (for local API)
// ======================================
// Quick mock API for testing:

/*
1. Install json-server globally:
   npm install -g json-server

2. Create db.json:
{
  "auth": {
    "users": [
      {
        "id": "user_123",
        "email": "test@example.com",
        "password": "Password123",  // In real app, never store plain!
        "garage_name": "John's Auto",
        "owner_name": "John Doe",
        "role": "owner"
      }
    ]
  }
}

3. Run server:
   json-server --watch db.json --port 3000

4. Update API URL in app:
   static const String _baseUrl = 'http://localhost:3000/api/v1';

5. Test endpoints:
   POST   http://localhost:3000/auth/login
   POST   http://localhost:3000/auth/register
   GET    http://localhost:3000/auth/me
*/

// OPTION 3: Mock Server (Recommended)
// ====================================
// Use dio_mock_adapter for better control

import 'package:dio_mock_adapter/dio_mock_adapter.dart';

void setupMockAdapter() {
  final dio = Dio();
  final dioAdapter = DioAdapter();
  dio.httpClientAdapter = dioAdapter;
  
  // Mock successful login
  dioAdapter.onPost('/auth/login', (server) {
    return server.reply(
      200,
      {
        'token': 'mock_jwt_token',
        'user': {
          'id': '123',
          'email': 'test@example.com',
          'role': 'owner',
        },
      },
    );
  });
  
  // Mock registration
  dioAdapter.onPost('/auth/register', (server) {
    return server.reply(
      201,
      {
        'token': 'mock_jwt_token_new',
        'user': {
          'id': '124',
          'email': 'newuser@example.com',
          'role': 'owner',
        },
      },
    );
  });
  
  // Mock validation endpoint
  dioAdapter.onGet('/auth/me', (server) {
    return server.reply(200, {'status': 'valid'});
  });
}

// OPTION 4: Use Gherkin / BDD for E2E
// =====================================
// Example: features/auth.feature

/*
Feature: User Authentication
  Scenario: User logs in with valid credentials
    Given the app is on the login page
    When the user enters email "test@example.com"
    And the user enters password "Password123"
    And the user taps the login button
    Then the user should be redirected to dashboard
    And the JWT token should be stored securely
    
  Scenario: User registers new account
    Given the app is on the signup page
    When the user enters garage name "John's Auto"
    And the user enters owner name "John Doe"
    And the user enters email "john@example.com"
    And the user enters password "Password123"
    And the user enters confirm password "Password123"
    And the user taps create account button
    Then the user should be logged in automatically
    And the user should see the dashboard
*/

// Manual Testing Checklist
// =========================

/*
SPLASH SCREEN
✓ Animation plays for 1.5 seconds
✓ Logo fade-in is smooth
✓ With valid token → redirects to dashboard
✓ Without token → redirects to login
✓ Invalid token → shows login

LOGIN SCREEN
✓ Email field validates format
✓ Password field shows toggle icon
✓ Submit invalid email → shows error
✓ Submit short password → shows error
✓ Submit valid credentials → loading spinner appears
✓ Success → redirects to dashboard
✓ Email already exists → shows error message
✓ Network error → shows timeout message
✓ "Sign up" link navigates to signup page
✓ "Forgot password" button exists (can be disabled)
✓ Button disabled during loading
✓ Error message disappears after 3 seconds

SIGN UP SCREEN
✓ All 5 fields are present
✓ Garage name required validation
✓ Owner name required validation
✓ Email format validation
✓ Password strength indicator updates in real-time
✓ Password min 8 chars requirement shown
✓ Uppercase letter requirement shown
✓ Number requirement shown
✓ Confirm password must match
✓ Submit valid form → loading spinner
✓ Success → auto-login, redirects to dashboard
✓ Email already exists → "Email already registered"
✓ "Login" link navigates back to login page
✓ Back arrow navigates to login

SECURITY
✓ JWT token stored in secure storage
✓ Token sent in Authorization header
✓ After logout, token is deleted
✓ Kill app, restart → redirects to login (token gone)
✓ Invalid token response → logout and redirect
✓ Device secure storage verified (check for encryption)

NETWORK
✓ Offline mode → shows network error
✓ Timeout (>30s) → shows timeout message
✓ Server 500 error → shows retry message
✓ Server 400 error → shows message from response
✓ Retry works 3 times automatically

STATE MANAGEMENT
✓ Hot reload → state preserved if cubit still alive
✓ Navigation back → form fields remember input
✓ Error message → only shows for 3 seconds
✓ Loading state → button disabled, spinner shown

RESPONSIVE DESIGN
✓ Phone (375 x 667) → readable, centered
✓ Tablet (600 x 960) → looks good
✓ Landscape (667 x 375) → no overflow
✓ Keyboard open → content scrolls up
*/

// Sample Test Data
// =================

class TestData {
  // Valid login credentials
  static const String validEmail = 'test@example.com';
  static const String validPassword = 'Password123';
  
  // Valid registration
  static const String garageName = "John's Auto Repair";
  static const String ownerName = 'John Doe';
  static const String newEmail = 'john@example.com';
  static const String newPassword = 'SecurePass123';
  
  // Mock JWT token (decoded: {"sub":"123","email":"test@example.com","iat":1234567890})
  static const String mockToken = 
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjMiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJpYXQiOjEyMzQ1Njc4OTB9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';
  
  // Invalid test cases
  static const String invalidEmail = 'notanemail';
  static const String shortPassword = 'pass';
  static const String noUppercasePassword = 'password123';
  static const String noNumberPassword = 'Password';
  
  // Error responses
  static final Map<String, dynamic> unauthorized401 = {
    'error': 'Unauthorized',
    'message': 'Invalid email or password'
  };
  
  static final Map<String, dynamic> conflict409 = {
    'error': 'Conflict',
    'message': 'Email already registered'
  };
  
  static final Map<String, dynamic> badRequest400 = {
    'error': 'Bad Request',
    'message': 'Validation error: email must be valid'
  };
}

// Running Tests
// ==============

/*
1. Unit tests (models, repository):
   flutter test test/features/auth/data/repositories/auth_repository_test.dart
   
2. Widget tests (UI):
   flutter test test/features/auth/presentation/pages/login_page_test.dart
   
3. Cubit tests (state management):
   flutter test test/features/auth/presentation/bloc/auth_cubit_test.dart
   
4. All tests:
   flutter test --coverage
   
5. Generate coverage report:
   genhtml coverage/lcov.info -o coverage/html
   open coverage/html/index.html
*/
