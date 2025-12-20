class DashboardData {
  final DashboardStats stats;
  final List<Organization> organizations;
  final UserProfile userProfile;

  DashboardData({
    required this.stats,
    required this.organizations,
    required this.userProfile,
  });
}

class DashboardStats {
  final int todaySessions;
  final int activeJobCards;
  final int pendingAppointments;
  final int carsInGarage;

  DashboardStats({
    required this.todaySessions,
    required this.activeJobCards,
    required this.pendingAppointments,
    required this.carsInGarage,
  });

  factory DashboardStats.empty() {
    return DashboardStats(
      todaySessions: 0,
      activeJobCards: 0,
      pendingAppointments: 0,
      carsInGarage: 0,
    );
  }
}

class Organization {
  final String id;
  final String name;
  final String? description;
  final DateTime createdAt;

  Organization({
    required this.id,
    required this.name,
    this.description,
    required this.createdAt,
  });

  factory Organization.fromJson(Map<String, dynamic> json) {
    return Organization(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}

class UserProfile {
  final String id;
  final String email;
  final String fullName;

  UserProfile({
    required this.id,
    required this.email,
    required this.fullName,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    return UserProfile(
      id: json['id'] as String,
      email: json['email'] as String,
      fullName: json['fullName'] as String,
    );
  }
}
