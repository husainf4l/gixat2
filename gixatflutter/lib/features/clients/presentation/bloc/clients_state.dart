part of 'clients_cubit.dart';

abstract class ClientsState extends Equatable {
  const ClientsState();

  @override
  List<Object?> get props => [];
}

class ClientsInitial extends ClientsState {}

class ClientsLoading extends ClientsState {}

class ClientsLoaded extends ClientsState {
  const ClientsLoaded({this.customers = const []});
  final List<dynamic> customers;

  @override
  List<Object?> get props => [customers];
}

class ClientsError extends ClientsState {
  const ClientsError({required this.message});
  final String message;

  @override
  List<Object?> get props => [message];
}

// State for create customer flow
class CreateCustomerLoading extends ClientsState {}

class CreateCustomerSuccess extends ClientsState {}
