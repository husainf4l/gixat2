class Customer {
  final String id;
  final String firstName;
  final String lastName;
  final String phoneNumber;
  final String? email;

  Customer({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.phoneNumber,
    this.email,
  });

  String get fullName => '$firstName $lastName';

  factory Customer.fromJson(Map<String, dynamic> json) {
    return Customer(
      id: json['id'],
      firstName: json['firstName'],
      lastName: json['lastName'],
      phoneNumber: json['phoneNumber'],
      email: json['email'],
    );
  }
}
