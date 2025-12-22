import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:graphql_flutter/graphql_flutter.dart';
import '../../../../core/graphql/car_queries.dart';

// States
abstract class CreateCarState {}

class CreateCarInitial extends CreateCarState {}

class CreateCarLoading extends CreateCarState {}

class CreateCarSuccess extends CreateCarState {}

class CreateCarError extends CreateCarState {
  final String message;
  CreateCarError(this.message);
}

// Cubit
class CreateCarCubit extends Cubit<CreateCarState> {
  final GraphQLClient client;

  CreateCarCubit({required this.client}) : super(CreateCarInitial());

  Future<void> createCar({
    required String customerId,
    required String make,
    required String model,
    int? year,
    required String licensePlate,
    String? vin,
    String? color,
  }) async {
    emit(CreateCarLoading());

    try {
      print('📝 Creating car for customer: $customerId');
      print('   Make: $make, Model: $model, Plate: $licensePlate');

      final result = await client.mutate(
        MutationOptions(
          document: gql(createCarMutation),
          variables: {
            'input': {
              'customerId': customerId,
              'make': make,
              'model': model,
              if (year != null) 'year': year,
              'licensePlate': licensePlate,
              if (vin != null) 'vin': vin,
              if (color != null) 'color': color,
            }
          },
        ),
      );

      if (result.hasException) {
        print('❌ Error creating car: ${result.exception}');
        emit(CreateCarError(result.exception.toString()));
        return;
      }

      print('✅ Car created successfully');
      emit(CreateCarSuccess());
    } catch (e) {
      print('❌ Exception creating car: $e');
      emit(CreateCarError(e.toString()));
    }
  }
}
