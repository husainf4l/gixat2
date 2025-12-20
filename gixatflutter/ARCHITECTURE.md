# 🏗️ Gixat Architecture Documentation

## Overview

**Gixat** follows **Clean Architecture** principles with **Bloc/Cubit** for state management. This ensures:
- ✅ Testable code
- ✅ Maintainable structure
- ✅ Scalable for enterprise
- ✅ Clear separation of concerns

---

## Architecture Layers

### 1. **Presentation Layer** (`lib/features/auth/presentation/`)

#### Purpose
UI components and state management that users interact with.

#### Components

**a) Bloc/Cubit** (`lib/features/auth/presentation/bloc/`)
```
AuthCubit
├── Methods:
│   ├── checkAuth()      → Validate token on startup
│   ├── login()          → Call repository + emit state
│   ├── register()       → Call repository + emit state
│   └── logout()         → Call repository + emit state
└── States (in auth_state.dart):
    ├── AuthInitial         → App just started
    ├── AuthLoading         → API call in progress
    ├── AuthAuthenticated   → Has valid token
    ├── AuthUnauthenticated → No token or invalid
    └── AuthError           → Something failed
```

**Why Cubit over Bloc?**
- Simpler (no events needed)
- Fewer boilerplate
- Perfect for auth (limited, sequential flows)
- Still fully testable

**b) Pages** (`lib/features/auth/presentation/pages/`)
```
splash_page.dart
├── Animation: FadeTransition (1.5s)
├── Logic: Call AuthCubit.checkAuth()
└── Navigation: Automatic (via router)

login_page.dart
├── Form fields: Email, Password
├── Validation: Client-side
├── Button state: Responds to AuthCubit
└── Error handling: Shows snackbar

signup_page.dart
├── Form fields: Garage name, Owner name, Email, Password, Confirm
├── Real-time requirements: Password strength indicator
├── Validation: All fields checked
└── Navigation: Auto-login on success
```

---

### 2. **Data Layer** (`lib/features/auth/data/`)

#### Purpose
Handle API communication, data transformation, and persistence.

#### Components

**a) Models** (`lib/features/auth/data/models/user_model.dart`)
```dart
User
├── id: String
├── email: String
├── role: String (e.g., 'owner')
└── Methods:
    ├── fromJson()  → API response → User object
    └── toJson()    → User object → JSON

AuthResponse
├── token: String
├── user: User
└── Methods:
    └── fromJson()  → API response → AuthResponse
```

**b) Repository** (`lib/features/auth/data/repositories/auth_repository.dart`)
```dart
AuthRepository
├── Methods:
│   ├── login()         → POST /auth/login
│   ├── register()      → POST /auth/register
│   ├── isTokenValid()  → Check token validity
│   ├── logout()        → Clear all stored data
│   └── getStoredToken()→ Retrieve JWT
└── Error Handling:
    ├── 400 → Extract message
    ├── 401 → Clear storage
    ├── 409 → Email exists
    └── Network → User-friendly message
```

**Why separate Repository?**
- Single source of truth
- Easy to mock for testing
- Can swap implementations
- Clean API interface

---

### 3. **Core Layer** (`lib/core/`)

#### Purpose
Shared utilities, theme, networking, storage.

**a) Theme** (`lib/core/theme/app_theme.dart`)
- Material 3 configuration
- Colors: Primary (Blue #3B82F6), Secondary (Slate), Error, Success
- Typography: Headlines, Body, Label (Inter font)
- Spacing constants: 8, 12, 16, 20, 24, 32
- Button/Input styles
- Border radius: 8, 12, 16

**b) Network** (`lib/core/network/network_client.dart`)
```dart
NetworkClient (wraps Dio)
├── Features:
│   ├── Base URL configuration
│   ├── Timeout handling (30s)
│   ├── Automatic retry (3 times)
│   └── Auth interceptor (adds Bearer token)
├── Error handling:
│   ├── Network timeouts
│   ├── 401 Unauthorized → logout
│   ├── Server errors (5xx)
│   └── Bad responses (4xx)
└── Methods:
    ├── get<T>()
    ├── post<T>()
    ├── put<T>()
    └── delete<T>()
```

**c) Storage** (`lib/core/storage/secure_storage_service.dart`)
```dart
SecureStorageService
├── Uses: flutter_secure_storage (encrypted)
├── Methods:
│   ├── saveToken()      → Encrypt & store JWT
│   ├── getToken()       → Retrieve JWT
│   ├── deleteToken()    → Remove JWT
│   ├── hasToken()       → Check existence
│   ├── saveUserId()     → Store user ID
│   ├── saveUserRole()   → Store user role
│   └── clearAll()       → Wipe all data on logout
└── Security:
    ├── Uses Keychain (iOS)
    ├── Uses Keystore (Android)
    └── Encrypted at rest
```

**d) Widgets** (`lib/core/widgets/gixat_widgets.dart`)
```dart
GixatTextField
├── Props: label, hint, controller, validator, etc.
├── Features:
│   ├── Custom styling (no Material defaults)
│   ├── Password toggle icon
│   ├── Error message display
│   └── Enable/disable state
└── Uses: AppTheme for consistency

GixatButton
├── Props: label, onPressed, isLoading, isEnabled
├── Features:
│   ├── Loading spinner
│   ├── Disabled state
│   ├── Custom sizing
│   └── Rounded corners
└── Uses: AppTheme colors

ErrorWidget, LoadingWidget, Snackbar helpers
```

---

### 4. **Router Layer** (`lib/router/app_router.dart`)

#### Purpose
Navigation and auth guard logic.

```dart
appRouter (GoRouter)
├── Routes:
│   ├── /splash     → SplashPage
│   ├── /login      → LoginPage
│   ├── /signup     → SignUpPage
│   └── /dashboard  → DashboardPage (authenticated)
├── Redirect Logic:
│   ├── If AuthAuthenticated → Go to /dashboard
│   ├── If AuthUnauthenticated → Go to /login
│   └── If AuthLoading → Stay on current
└── Features:
    ├── No Navigator.push() hacks
    ├── Centralized routing
    ├── URL-based navigation (deep linking ready)
    └── Automatic auth guard
```

---

## Data Flow Diagram

### Login Flow:
```
LoginPage
    ↓
  User enters email & password
    ↓
  Form validation (client-side)
    ↓
  Button press → context.read<AuthCubit>().login()
    ↓
  AuthCubit emits AuthLoading
    ↓
  AuthRepository.login() calls NetworkClient.post()
    ↓
  NetworkClient (with auth interceptor)
    ├── Adds "Authorization: Bearer <existing_token>" if any
    └── Sends POST /auth/login
    ↓
  Server responds:
    ├── ✓ 200: AuthResponse with token + user
    │   ├── AuthRepository saves token → SecureStorageService
    │   ├── AuthCubit emits AuthAuthenticated
    │   └── GoRouter redirects to /dashboard
    │
    └── ✗ Error:
        ├── Parse error → Extract message
        ├── AuthCubit emits AuthError
        └── Page shows snackbar with message
```

### Splash Screen Flow:
```
App starts
    ↓
  main.dart initializes AuthCubit
    ↓
  GoRouter navigates to /splash
    ↓
  SplashPage initiated → _checkAuthentication()
    ↓
  500ms delay (animation plays)
    ↓
  AuthCubit.checkAuth() called:
    ├── Get stored token from SecureStorageService
    ├── Make GET /auth/me with token
    ├── If 200 → AuthCubit emits AuthAuthenticated
    ├── If 401 or no token → AuthCubit emits AuthUnauthenticated
    └── If error → AuthCubit emits AuthError
    ↓
  GoRouter redirect:
    ├── AuthAuthenticated → Go to /dashboard
    └── AuthUnauthenticated → Go to /login
```

---

## State Management Pattern

### Why Cubit?

| Aspect | Bloc | Cubit |
|--------|------|-------|
| Events | Yes | No |
| Methods | No | Yes |
| Complexity | Higher | Lower |
| Boilerplate | More | Less |
| Auth use case | Overkill | Perfect ✅ |

### Auth States

```dart
// 1. Initial
AuthInitial
  → App just started, no checks done yet

// 2. Loading
AuthLoading
  → API call in progress, disable UI

// 3. Success
AuthAuthenticated
  → Valid token, user can access app

// 4. Failure
AuthUnauthenticated
  → No token or token expired
  → Send to login

// 5. Exception
AuthError(message)
  → Network error, server error
  → Show snackbar with friendly message
```

---

## Error Handling Strategy

### Network Errors

| Error Type | Status | Handling | User Message |
|------------|--------|----------|--------------|
| Connection timeout | - | Retry 3x | "Connection timeout. Check internet." |
| No internet | - | Retry 3x | "Network error. Check connection." |
| Bad request | 400 | Show message | From API response |
| Unauthorized | 401 | Clear token, logout | "Invalid email or password" |
| Conflict | 409 | Show message | "Email already registered" |
| Server error | 5xx | Retry 3x | "Server error. Try again." |

### Validation Errors

**Login:**
- Email: Required + valid format
- Password: Required + 6+ chars

**Signup:**
- Garage name: Required
- Owner name: Required
- Email: Required + valid format
- Password: Required + 8 chars + 1 uppercase + 1 number
- Confirm: Must match password

---

## Dependency Injection

```dart
// main.dart
void main() async {
  // 1. Initialize storage
  final storage = SecureStorageService();
  
  // 2. Initialize network with storage
  final networkClient = NetworkClient(storage: storage);
  
  // 3. Create repository with network + storage
  final authRepository = AuthRepository(
    networkClient: networkClient,
    storage: storage,
  );
  
  // 4. Provide AuthCubit to widget tree
  runApp(
    BlocProvider(
      create: (context) => AuthCubit(authRepository: authRepository),
      child: GixatApp(authRepository: authRepository),
    ),
  );
}
```

**Benefits:**
- Easy to test (mock dependencies)
- Easy to swap implementations
- Single source of truth
- No global state

---

## Security Measures

### ✅ Implemented
- Secure storage (encrypted at OS level)
- Bearer token in Authorization header
- Auto-logout on 401
- Clear data on logout
- Token validation on startup

### 📋 Recommended for Production
- Implement token refresh (if using short-lived tokens)
- Add certificate pinning
- Add request signing
- Implement biometric unlock
- Rate limiting on client side
- Session timeout

---

## Testing Strategy

### Unit Tests (Repository)
```dart
test('login returns AuthResponse on success', () async {
  // Arrange: Mock NetworkClient
  final mockNetworkClient = MockNetworkClient();
  when(mockNetworkClient.post(...)).thenAnswer(...)
  
  final repo = AuthRepository(
    networkClient: mockNetworkClient,
    storage: mockStorage,
  );
  
  // Act
  final result = await repo.login(email: '...', password: '...');
  
  // Assert
  expect(result, isA<AuthResponse>());
  expect(result.token, isNotEmpty);
});
```

### Widget Tests (UI)
```dart
testWidgets('LoginPage shows error on invalid email', (tester) async {
  // Arrange
  await tester.pumpWidget(GixatApp(...));
  
  // Act
  await tester.enterText(find.byType(GixatTextField), 'invalid');
  await tester.tap(find.byType(GixatButton));
  await tester.pumpAndSettle();
  
  // Assert
  expect(find.text('Enter a valid email'), findsOneWidget);
});
```

### Cubit Tests
```dart
blocTest<AuthCubit, AuthState>(
  'emits [AuthLoading, AuthAuthenticated] on successful login',
  build: () => AuthCubit(authRepository: mockRepo),
  act: (cubit) => cubit.login(email: '...', password: '...'),
  expect: () => [
    const AuthLoading(),
    const AuthAuthenticated(),
  ],
);
```

---

## Performance Optimization

### Current Optimizations
- ✅ Lazy initialization (services created in main)
- ✅ Const constructors throughout
- ✅ SingleTickerProviderStateMixin for animation
- ✅ Dispose controllers properly
- ✅ Avoid rebuilds with BlocBuilder

### Future Optimizations
- [ ] Offline caching layer
- [ ] Request caching
- [ ] Image caching
- [ ] Code splitting
- [ ] Lazy load features

---

## Scaling Strategy

### Current Scope
- Auth module only (splash, login, signup)
- No user profile, no settings, no dashboard logic

### To Add New Feature (e.g., Dashboard)

1. **Create feature directory**
   ```
   lib/features/dashboard/
   ├── data/
   ├── presentation/
   └── ...
   ```

2. **Create feature Cubit**
   ```dart
   class DashboardCubit extends Cubit<DashboardState> { ... }
   ```

3. **Create repository**
   ```dart
   class DashboardRepository { ... }
   ```

4. **Create UI pages**
   ```dart
   class DashboardPage extends StatelessWidget { ... }
   ```

5. **Register routes**
   ```dart
   // In app_router.dart
   GoRoute(path: '/dashboard', builder: ...)
   ```

6. **Provide Cubit**
   ```dart
   // In main.dart
   BlocProvider(
     create: (context) => DashboardCubit(...),
     child: ...
   )
   ```

---

## File Naming Conventions

- **Models**: `user_model.dart`
- **Repositories**: `auth_repository.dart`
- **Cubits**: `auth_cubit.dart`
- **Pages**: `login_page.dart`
- **Widgets**: `gixat_widgets.dart` or `custom_widget.dart`
- **Services**: `secure_storage_service.dart`
- **Theme**: `app_theme.dart`
- **Router**: `app_router.dart`

## Summary

This architecture provides:
- ✅ **Maintainability**: Clear layers, easy to find code
- ✅ **Testability**: Mock-friendly dependencies
- ✅ **Scalability**: Easy to add features
- ✅ **Security**: Encrypted storage, auth guard
- ✅ **Performance**: Optimized, minimal rebuilds
- ✅ **UX**: Smooth animations, friendly errors
- ✅ **Professional**: Enterprise-ready code

---

**Ready to scale!** 🚀
