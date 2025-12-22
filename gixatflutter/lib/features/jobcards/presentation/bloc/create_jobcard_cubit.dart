import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import '../../../../core/graphql/session_queries.dart';
import '../../../../core/graphql/jobcard_queries.dart';

// States
abstract class CreateJobCardState {}

class CreateJobCardInitial extends CreateJobCardState {}

class CreateJobCardLoading extends CreateJobCardState {}

class CreateJobCardLoaded extends CreateJobCardState {
  final List<Map<String, dynamic>> sessions;

  CreateJobCardLoaded({required this.sessions});
}

class CreateJobCardSuccess extends CreateJobCardState {}

class CreateJobCardError extends CreateJobCardState {
  final String message;
  CreateJobCardError(this.message);
}

// Cubit
class CreateJobCardCubit extends Cubit<CreateJobCardState> {
  final GraphQLClient client;

  CreateJobCardCubit({required this.client}) : super(CreateJobCardInitial());

  Future<void> loadSessions() async {
    emit(CreateJobCardLoading());
    
    try {
      final result = await client.query(
        QueryOptions(
          document: gql(getSessionsQuery),
          variables: {'first': 100},
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (result.hasException) {
        print('❌ Error loading sessions: ${result.exception}');
        emit(CreateJobCardError(result.exception.toString()));
        return;
      }

      final edges = result.data?['sessions']?['edges'] as List<dynamic>? ?? [];
      final sessions = edges.map((edge) {
        final node = edge['node'] as Map<String, dynamic>;
        return {
          'id': node['id'],
          'status': node['status'],
          'customer': node['customer'],
          'car': node['car'],
        };
      }).toList();

      print('✅ Loaded ${sessions.length} sessions');
      emit(CreateJobCardLoaded(sessions: sessions));
    } catch (e) {
      print('❌ Exception loading sessions: $e');
      emit(CreateJobCardError(e.toString()));
    }
  }

  Future<void> createJobCard({
    required String sessionId,
    String? description,
    double? estimatedCost,
  }) async {
    try {
      print('📝 Creating job card for session: $sessionId');
      
      final result = await client.mutate(
        MutationOptions(
          document: gql(createJobCardMutation),
          variables: {
            'sessionId': sessionId,
            if (description != null) 'description': description,
            if (estimatedCost != null) 'estimatedCost': estimatedCost,
          },
        ),
      );

      if (result.hasException) {
        print('❌ Error creating job card: ${result.exception}');
        emit(CreateJobCardError(result.exception.toString()));
        return;
      }

      print('✅ Job card created successfully');
      emit(CreateJobCardSuccess());
    } catch (e) {
      print('❌ Exception creating job card: $e');
      emit(CreateJobCardError(e.toString()));
    }
  }
}
