import 'package:equatable/equatable.dart';

class User extends Equatable {
  final String id;
  final String email;
  final String role;

  const User({
    required this.id,
    required this.email,
    required this.role,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['id'] ?? '',
      email: json['email'] ?? '',
      role: json['role'] ?? 'owner',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'role': role,
    };
  }

  @override
  List<Object?> get props => [id, email, role];
}

class AuthResponse extends Equatable {
  final String token;
  final User user;

  const AuthResponse({
    required this.token,
    required this.user,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      token: json['token'] ?? '',
      user: User.fromJson(json['user'] ?? {}),
    );
  }

  @override
  List<Object?> get props => [token, user];
}
