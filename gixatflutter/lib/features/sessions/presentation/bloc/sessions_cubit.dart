import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../data/models/session_model.dart';
import '../../data/repositories/sessions_repository.dart';

part 'sessions_state.dart';

class SessionsCubit extends Cubit<SessionsState> {
  SessionsCubit({required SessionsRepository repository})
      : _repository = repository,
        super(const SessionsInitial());

  final SessionsRepository _repository;

  Future<void> loadSessions({
    bool refresh = false,
    Map<String, dynamic>? where,
  }) async {
    if (isClosed) return; // Don't emit if cubit is closed
    
    if (refresh) {
      emit(const SessionsLoading());
    } else if (state is! SessionsLoaded) {
      emit(const SessionsLoading());
    }

    try {
      final response = await _repository.getSessions(
        first: 20,
        where: where,
        order: [
          {'createdAt': 'DESC'}
        ],
      );

      if (isClosed) return; // Don't emit if cubit is closed
      
      emit(SessionsLoaded(
        sessions: response.sessions,
        totalCount: response.totalCount,
        hasNextPage: response.hasNextPage,
        endCursor: response.endCursor,
      ));
    } catch (e) {
      if (isClosed) return; // Don't emit if cubit is closed
      emit(SessionsError(message: e.toString()));
    }
  }

  Future<void> loadMore() async {
    if (state is! SessionsLoaded) return;

    final currentState = state as SessionsLoaded;
    if (!currentState.hasNextPage || currentState.isLoadingMore) return;

    emit(currentState.copyWith(isLoadingMore: true));

    try {
      final response = await _repository.getSessions(
        first: 20,
        after: currentState.endCursor,
        order: [
          {'createdAt': 'DESC'}
        ],
      );

      emit(SessionsLoaded(
        sessions: [...currentState.sessions, ...response.sessions],
        totalCount: response.totalCount,
        hasNextPage: response.hasNextPage,
        endCursor: response.endCursor,
        isLoadingMore: false,
      ));
    } catch (e) {
      emit(currentState.copyWith(isLoadingMore: false));
    }
  }
}
