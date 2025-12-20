import 'customer.dart';

class Car {
  Car({
    required this.id,
    required this.make,
    required this.model,
    required this.year,
    required this.licensePlate,
    this.vin,
    this.color,
    this.customer,
  });

  factory Car.fromJson(Map<String, dynamic> json) => Car(
        id: json['id'],
        make: json['make'],
        model: json['model'],
        year: json['year'],
        licensePlate: json['licensePlate'],
        vin: json['vin'],
        color: json['color'],
        customer: json['customer'] != null
            ? Customer.fromJson(json['customer'])
            : null,
      );

  final String id;
  final String make;
  final String model;
  final int year;
  final String licensePlate;
  final String? vin;
  final String? color;
  final Customer? customer;

  String get displayName => '$year $make $model ($licensePlate)';
}
