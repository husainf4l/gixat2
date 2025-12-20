import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../data/models/dashboard_data.dart';
import '../../data/repositories/dashboard_repository.dart';

// States
abstract class DashboardState extends Equatable {
  @override
  List<Object?> get props => [];
}

class DashboardInitial extends DashboardState {
  @override
  List<Object?> get props => [];
}

class DashboardLoading extends DashboardState {
  @override
  List<Object?> get props => [];
}

class DashboardLoaded extends DashboardState {
  DashboardLoaded(this.data);
  final DashboardData data;

  @override
  List<Object?> get props => [data];
}

class DashboardError extends DashboardState {
  DashboardError(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}

// Cubit
class DashboardCubit extends Cubit<DashboardState> {
  DashboardCubit({required this.repository}) : super(DashboardInitial());
  final DashboardRepository repository;

  Future<void> loadDashboard() async {
    emit(DashboardLoading());
    try {
      final data = await repository.getDashboardData();
      emit(DashboardLoaded(data));
    } catch (e) {
      emit(DashboardError(e.toString()));
    }
  }

  Future<void> refreshDashboard() async {
    try {
      final data = await repository.getDashboardData();
      emit(DashboardLoaded(data));
    } catch (e) {
      emit(DashboardError(e.toString()));
    }
  }
}
