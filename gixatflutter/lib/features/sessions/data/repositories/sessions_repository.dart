import '../datasources/sessions_remote_data_source.dart';
import '../models/session_model.dart';

class SessionsRepository {
  SessionsRepository({SessionsRemoteDataSource? dataSource})
      : _dataSource = dataSource ?? SessionsRemoteDataSource();

  final SessionsRemoteDataSource _dataSource;

  Future<SessionsResponse> getSessions({
    int first = 20,
    String? after,
    Map<String, dynamic>? where,
    List<Map<String, dynamic>>? order,
  }) async {
    try {
      final data = await _dataSource.getSessions(
        first: first,
        after: after,
        where: where,
        order: order,
      );

      final edges = data['edges'] as List<dynamic>? ?? [];
      final sessions = edges
          .map((edge) => GarageSession.fromJson(edge['node'] as Map<String, dynamic>))
          .toList();

      final pageInfo = data['pageInfo'] as Map<String, dynamic>? ?? {};
      final totalCount = data['totalCount'] as int? ?? 0;

      return SessionsResponse(
        sessions: sessions,
        totalCount: totalCount,
        hasNextPage: pageInfo['hasNextPage'] as bool? ?? false,
        endCursor: pageInfo['endCursor'] as String?,
      );
    } catch (e) {
      print('❌ Repository error: $e');
      rethrow;
    }
  }

  Future<GarageSession> getSessionById(String id) async {
    final data = await _dataSource.getSessionById(id);
    return GarageSession.fromJson(data);
  }
}
