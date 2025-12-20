import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../data/repositories/auth_repository.dart';

part 'auth_state.dart';

class AuthCubit extends Cubit<AuthState> {
  AuthCubit({required AuthRepository authRepository})
      : _authRepository = authRepository,
        super(const AuthInitial());
  final AuthRepository _authRepository;

  /// Check authentication on app startup
  Future<void> checkAuth() async {
    emit(const AuthLoading());
    try {
      final isValid = await _authRepository.isTokenValid();
      if (isValid) {
        final token = await _authRepository.getStoredToken();
        if (token != null) {
          emit(const AuthAuthenticated());
          return;
        }
      }
      emit(const AuthUnauthenticated());
    } catch (e) {
      emit(AuthUnauthenticated(message: e.toString()));
    }
  }

  /// Login with email and password
  Future<void> login({
    required String email,
    required String password,
  }) async {
    emit(const AuthLoading());
    try {
      await _authRepository.login(
        email: email,
        password: password,
      );
      emit(const AuthAuthenticated());
    } catch (e) {
      final message = e.toString().replaceFirst('Exception: ', '');
      emit(AuthError(message: message));
    }
  }

  /// Register new account
  Future<void> register({
    required String garageName,
    required String ownerName,
    required String email,
    required String password,
  }) async {
    emit(const AuthLoading());
    try {
      await _authRepository.register(
        garageName: garageName,
        ownerName: ownerName,
        email: email,
        password: password,
      );
      emit(const AuthAuthenticated());
    } catch (e) {
      final message = e.toString().replaceFirst('Exception: ', '');
      print('AUTH ERROR: $message');
      emit(AuthError(message: message));
    }
  }

  /// Logout
  Future<void> logout() async {
    emit(const AuthLoading());
    try {
      await _authRepository.logout();
      emit(const AuthUnauthenticated());
    } catch (e) {
      emit(AuthError(message: e.toString()));
    }
  }
}
