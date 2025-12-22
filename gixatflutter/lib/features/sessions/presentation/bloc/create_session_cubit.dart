import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import '../../../../core/graphql/customer_queries.dart';
import '../../../../core/graphql/car_queries.dart';
import '../../../../core/graphql/session_queries.dart';

// States
abstract class CreateSessionState {}

class CreateSessionInitial extends CreateSessionState {}

class CreateSessionLoading extends CreateSessionState {}

class CreateSessionLoaded extends CreateSessionState {
  final List<Map<String, dynamic>> customers;
  final List<Map<String, dynamic>> cars;

  CreateSessionLoaded({
    required this.customers,
    this.cars = const [],
  });

  CreateSessionLoaded copyWith({
    List<Map<String, dynamic>>? customers,
    List<Map<String, dynamic>>? cars,
  }) {
    return CreateSessionLoaded(
      customers: customers ?? this.customers,
      cars: cars ?? this.cars,
    );
  }
}

class CreateSessionSuccess extends CreateSessionState {}

class CreateSessionError extends CreateSessionState {
  final String message;
  CreateSessionError(this.message);
}

// Cubit
class CreateSessionCubit extends Cubit<CreateSessionState> {
  final GraphQLClient client;

  CreateSessionCubit({required this.client}) : super(CreateSessionInitial());

  Future<void> loadCustomers() async {
    emit(CreateSessionLoading());
    
    try {
      final result = await client.query(
        QueryOptions(
          document: gql(getCustomersQuery),
          variables: {'first': 100},
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (result.hasException) {
        print('❌ Error loading customers: ${result.exception}');
        emit(CreateSessionError(result.exception.toString()));
        return;
      }

      final edges = result.data?['customers']?['edges'] as List<dynamic>? ?? [];
      final customers = edges.map((edge) {
        final node = edge['node'] as Map<String, dynamic>;
        return {
          'id': node['id'],
          'firstName': node['firstName'],
          'lastName': node['lastName'],
          'email': node['email'],
          'phoneNumber': node['phoneNumber'],
        };
      }).toList();

      print('✅ Loaded ${customers.length} customers');
      emit(CreateSessionLoaded(customers: customers));
    } catch (e) {
      print('❌ Exception loading customers: $e');
      emit(CreateSessionError(e.toString()));
    }
  }

  Future<void> loadCarsForCustomer(String customerId) async {
    print('🔍 loadCarsForCustomer called with: $customerId');
    final currentState = state;
    print('🔍 Current state type: ${currentState.runtimeType}');
    if (currentState is! CreateSessionLoaded) {
      print('❌ State is not CreateSessionLoaded, returning');
      return;
    }

    try {
      print('🔍 Querying cars with customerId: $customerId');
      final result = await client.query(
        QueryOptions(
          document: gql(getCarsQuery),
          variables: {
            'first': 100,
            'where': {
              'customerId': {'eq': customerId}
            },
          },
          fetchPolicy: FetchPolicy.networkOnly,
        ),
      );

      if (result.hasException) {
        print('❌ Error loading cars: ${result.exception}');
        return;
      }

      final edges = result.data?['cars']?['edges'] as List<dynamic>? ?? [];
      final cars = edges.map((edge) {
        final node = edge['node'] as Map<String, dynamic>;
        return {
          'id': node['id'],
          'make': node['make'],
          'model': node['model'],
          'year': node['year'],
          'licensePlate': node['licensePlate'],
          'vin': node['vin'],
          'color': node['color'],
        };
      }).toList();

      print('✅ Loaded ${cars.length} cars for customer');
      emit(currentState.copyWith(cars: cars));
    } catch (e) {
      print('❌ Exception loading cars: $e');
    }
  }

  Future<void> createSession({
    required String customerId,
    required String carId,
  }) async {
    try {
      print('📝 Creating session for customer: $customerId, car: $carId');
      
      final result = await client.mutate(
        MutationOptions(
          document: gql(createSessionMutation),
          variables: {
            'carId': carId,
            'customerId': customerId,
          },
        ),
      );

      if (result.hasException) {
        print('❌ Error creating session: ${result.exception}');
        emit(CreateSessionError(result.exception.toString()));
        return;
      }

      print('✅ Session created successfully');
      emit(CreateSessionSuccess());
    } catch (e) {
      print('❌ Exception creating session: $e');
      emit(CreateSessionError(e.toString()));
    }
  }
}
