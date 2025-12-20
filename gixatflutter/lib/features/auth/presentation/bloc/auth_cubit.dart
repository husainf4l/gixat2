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
          final hasGarage = await _authRepository.hasGarage();
          if (hasGarage) {
            emit(const AuthAuthenticated());
          } else {
            emit(const AuthNeedsGarage());
          }
          return;
        }
      }
      emit(const AuthUnauthenticated());
    } on Exception catch (e) {
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
      final hasGarage = await _authRepository.hasGarage();
      if (hasGarage) {
        emit(const AuthAuthenticated());
      } else {
        emit(const AuthNeedsGarage());
      }
    } on Exception catch (e) {
      final message = e.toString().replaceFirst('Exception: ', '');
      emit(AuthError(message: message));
    }
  }

  /// Register new account
  Future<void> register({
    required String ownerName,
    required String email,
    required String password,
  }) async {
    emit(const AuthLoading());
    try {
      await _authRepository.register(
        ownerName: ownerName,
        email: email,
        password: password,
      );
      // Registration currently doesn't link a garage in the mutation
      // so we emit AuthNeedsGarage to force the user to create/connect one
      emit(const AuthNeedsGarage());
    } on Exception catch (e) {
      final message = e.toString().replaceFirst('Exception: ', '');
      emit(AuthError(message: message));
    }
  }

  /// Logout
  Future<void> logout() async {
    emit(const AuthLoading());
    try {
      await _authRepository.logout();
      emit(const AuthUnauthenticated());
    } on Exception catch (e) {
      emit(AuthError(message: e.toString()));
    }
  }

  /// Select or create a garage
  Future<void> selectGarage(String garageId) async {
    emit(const AuthLoading());
    try {
      // In a real app, this would call an API to link the user to the garage
      // For now, we just save it locally
      await _authRepository.saveGarageId(garageId);
      emit(const AuthAuthenticated());
    } on Exception catch (e) {
      emit(AuthError(message: e.toString()));
    }
  }

  /// Create a new organization (garage)
  Future<void> createOrganization({
    required String name,
    required String country,
    required String city,
    required String street,
    required String phoneCountryCode,
  }) async {
    emit(const AuthLoading());
    try {
      await _authRepository.createOrganization(
        name: name,
        country: country,
        city: city,
        street: street,
        phoneCountryCode: phoneCountryCode,
      );
      emit(const AuthAuthenticated());
    } on Exception catch (e) {
      final message = e.toString().replaceFirst('Exception: ', '');
      emit(AuthError(message: message));
    }
  }
}
