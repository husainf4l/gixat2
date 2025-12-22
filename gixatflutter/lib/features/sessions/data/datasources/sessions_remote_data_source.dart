import 'package:graphql_flutter/graphql_flutter.dart';
import '../../../../core/graphql/graphql_client.dart';
import '../../../../core/graphql/session_queries.dart';

class SessionsRemoteDataSource {
  Future<Map<String, dynamic>> getSessions({
    int? first,
    String? after,
    Map<String, dynamic>? where,
    List<Map<String, dynamic>>? order,
  }) async {
    final client = await GraphQLConfig.getClient(withAuth: true);

    print('📡 Fetching sessions...');

    final result = await client.query(
      QueryOptions(
        document: gql(getSessionsQuery),
        variables: {
          if (first != null) 'first': first,
          if (after != null) 'after': after,
          if (where != null) 'where': where,
          if (order != null) 'order': order,
        },
        fetchPolicy: FetchPolicy.networkOnly,
      ),
    );

    if (result.hasException) {
      print('❌ Sessions query exception: ${result.exception}');
      throw result.exception!;
    }

    final data = result.data?['sessions'];
    if (data == null) {
      print('❌ No sessions data returned');
      throw Exception('Failed to fetch sessions');
    }

    print('✅ Sessions fetched: ${data['totalCount']} total');
    return data;
  }

  Future<Map<String, dynamic>> getSessionById(String id) async {
    final client = await GraphQLConfig.getClient(withAuth: true);

    final result = await client.query(
      QueryOptions(
        document: gql(getSessionByIdQuery),
        variables: {'id': id},
        fetchPolicy: FetchPolicy.networkOnly,
      ),
    );

    if (result.hasException) {
      throw result.exception!;
    }

    final data = result.data?['sessionById'];
    if (data == null) {
      throw Exception('Session not found');
    }

    return data;
  }
}
