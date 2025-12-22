import 'package:equatable/equatable.dart';

class GarageSession extends Equatable {
  const GarageSession({
    required this.id,
    required this.status,
    required this.createdAt,
    required this.car,
    required this.customer,
  });

  final String id;
  final String status;
  final DateTime createdAt;
  final Car car;
  final Customer customer;

  factory GarageSession.fromJson(Map<String, dynamic> json) {
    return GarageSession(
      id: json['id'] as String,
      status: json['status'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      car: Car.fromJson(json['car'] as Map<String, dynamic>),
      customer: Customer.fromJson(json['customer'] as Map<String, dynamic>),
    );
  }

  @override
  List<Object?> get props => [id, status, createdAt, car, customer];
}

class Car extends Equatable {
  const Car({
    required this.make,
    required this.model,
    required this.licensePlate,
  });

  final String make;
  final String model;
  final String licensePlate;

  factory Car.fromJson(Map<String, dynamic> json) {
    return Car(
      make: json['make'] as String? ?? '',
      model: json['model'] as String? ?? '',
      licensePlate: json['licensePlate'] as String? ?? '',
    );
  }

  @override
  List<Object?> get props => [make, model, licensePlate];
}

class Customer extends Equatable {
  const Customer({
    required this.firstName,
    required this.lastName,
  });

  final String firstName;
  final String lastName;

  String get fullName => '$firstName $lastName';

  factory Customer.fromJson(Map<String, dynamic> json) {
    return Customer(
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
    );
  }

  @override
  List<Object?> get props => [firstName, lastName];
}

class SessionsResponse extends Equatable {
  const SessionsResponse({
    required this.sessions,
    required this.totalCount,
    required this.hasNextPage,
    this.endCursor,
  });

  final List<GarageSession> sessions;
  final int totalCount;
  final bool hasNextPage;
  final String? endCursor;

  @override
  List<Object?> get props => [sessions, totalCount, hasNextPage, endCursor];
}
