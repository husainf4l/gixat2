class DashboardData {
  DashboardData({
    required this.stats,
    required this.organizations,
    required this.userProfile,
  });
  final DashboardStats stats;
  final List<Organization> organizations;
  final UserProfile userProfile;
}

class DashboardStats {
  DashboardStats({
    required this.todaySessions,
    required this.activeJobCards,
    required this.pendingAppointments,
    required this.carsInGarage,
  });

  factory DashboardStats.empty() => DashboardStats(
        todaySessions: 0,
        activeJobCards: 0,
        pendingAppointments: 0,
        carsInGarage: 0,
      );
  final int todaySessions;
  final int activeJobCards;
  final int pendingAppointments;
  final int carsInGarage;
}

class Organization {
  Organization({
    required this.id,
    required this.name,
    required this.createdAt,
    this.description,
  });

  factory Organization.fromJson(Map<String, dynamic> json) => Organization(
        id: json['id'] as String,
        name: json['name'] as String,
        description: json['description'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
  final String id;
  final String name;
  final String? description;
  final DateTime createdAt;
}

class UserProfile {
  UserProfile({
    required this.id,
    required this.email,
    required this.fullName,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) => UserProfile(
        id: json['id'] as String,
        email: json['email'] as String,
        fullName: json['fullName'] as String,
      );
  final String id;
  final String email;
  final String fullName;
}
