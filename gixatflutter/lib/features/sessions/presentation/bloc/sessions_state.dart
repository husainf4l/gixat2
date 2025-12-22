part of 'sessions_cubit.dart';

abstract class SessionsState extends Equatable {
  const SessionsState();

  @override
  List<Object?> get props => [];
}

class SessionsInitial extends SessionsState {
  const SessionsInitial();
}

class SessionsLoading extends SessionsState {
  const SessionsLoading();
}

class SessionsLoaded extends SessionsState {
  const SessionsLoaded({
    required this.sessions,
    required this.totalCount,
    required this.hasNextPage,
    this.endCursor,
    this.isLoadingMore = false,
  });

  final List<GarageSession> sessions;
  final int totalCount;
  final bool hasNextPage;
  final String? endCursor;
  final bool isLoadingMore;

  SessionsLoaded copyWith({
    List<GarageSession>? sessions,
    int? totalCount,
    bool? hasNextPage,
    String? endCursor,
    bool? isLoadingMore,
  }) {
    return SessionsLoaded(
      sessions: sessions ?? this.sessions,
      totalCount: totalCount ?? this.totalCount,
      hasNextPage: hasNextPage ?? this.hasNextPage,
      endCursor: endCursor ?? this.endCursor,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
    );
  }

  @override
  List<Object?> get props => [sessions, totalCount, hasNextPage, endCursor, isLoadingMore];
}

class SessionsError extends SessionsState {
  const SessionsError({required this.message});

  final String message;

  @override
  List<Object?> get props => [message];
}
