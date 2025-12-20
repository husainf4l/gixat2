import 'package:equatable/equatable.dart';

class User extends Equatable {
  const User({
    required this.id,
    required this.email,
    required this.role,
  });

  factory User.fromJson(Map<String, dynamic> json) => User(
        id: json['id'] ?? '',
        email: json['email'] ?? '',
        role: json['role'] ?? 'owner',
      );
  final String id;
  final String email;
  final String role;

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'role': role,
      };

  @override
  List<Object?> get props => [id, email, role];
}

class AuthResponse extends Equatable {
  const AuthResponse({
    required this.token,
    required this.user,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
        token: json['token'] ?? '',
        user: User.fromJson(json['user'] ?? {}),
      );
  final String token;
  final User user;

  @override
  List<Object?> get props => [token, user];
}

class UserInfo extends Equatable {
  const UserInfo({
    required this.id,
    required this.email,
    this.fullName,
    this.organizationId,
    this.organizationName,
  });

  final String id;
  final String email;
  final String? fullName;
  final String? organizationId;
  final String? organizationName;

  @override
  List<Object?> get props => [id, email, fullName, organizationId, organizationName];
}
