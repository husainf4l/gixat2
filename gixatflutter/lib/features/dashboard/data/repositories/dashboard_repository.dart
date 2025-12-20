import 'package:graphql_flutter/graphql_flutter.dart';
import '../models/dashboard_data.dart';
import '../../../../core/graphql/graphql_client.dart';

class DashboardRepository {
  Future<DashboardData> getDashboardData() async {
    try {
      final client = await GraphQLConfig.getClient(withAuth: true);

      // Get user profile
      final meResult = await client.query(
        QueryOptions(
          document: gql(r'''
            query Me {
              me {
                id
                email
                fullName
              }
            }
          '''),
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (meResult.hasException) {
        throw Exception('Failed to load user profile');
      }

      final userData = meResult.data?['me'];
      final userProfile = UserProfile.fromJson(userData);

      // Get organizations
      final orgsResult = await client.query(
        QueryOptions(
          document: gql(r'''
            query Organizations {
              organizations {
                id
                name
                description
                createdAt
              }
            }
          '''),
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      final List<Organization> organizations = [];
      if (orgsResult.data?['organizations'] != null) {
        for (var org in orgsResult.data!['organizations']) {
          organizations.add(Organization.fromJson(org));
        }
      }

      // For now, return empty stats since backend doesn't have these endpoints yet
      // When backend adds sessions, appointments, etc., we'll add those queries here
      final stats = DashboardStats.empty();

      return DashboardData(
        stats: stats,
        organizations: organizations,
        userProfile: userProfile,
      );
    } catch (e) {
      throw Exception('Failed to load dashboard data: ${e.toString()}');
    }
  }
}
