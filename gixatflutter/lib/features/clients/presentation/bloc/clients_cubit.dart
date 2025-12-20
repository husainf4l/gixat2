import 'package:equatable/equatable.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../data/repositories/clients_repository.dart';

part 'clients_state.dart';

class ClientsCubit extends Cubit<ClientsState> {
  ClientsCubit({
    required ClientsRepository clientsRepository,
  })  : _clientsRepository = clientsRepository,
        super(ClientsInitial());

  final ClientsRepository _clientsRepository;

  // Cache for lookup data
  List<Map<String, dynamic>> _countries = [];

  Future<void> createCustomer({
    required String firstName,
    required String lastName,
    required String phoneNumber,
    String? email,
    String? country,
    String? city,
    String? street,
  }) async {
    emit(CreateCustomerLoading());
    try {
      if (kDebugMode) {
        print('ClientsCubit: Creating customer...');
      }
      
      await _clientsRepository.createCustomer(
        firstName: firstName,
        lastName: lastName,
        phoneNumber: phoneNumber,
        email: email,
        country: country,
        city: city,
        street: street,
      );
      
      if (kDebugMode) {
        print('ClientsCubit: Customer created successfully');
      }
      
      emit(CreateCustomerSuccess());
    } on Exception catch (e, stackTrace) {
      if (kDebugMode) {
        print('ClientsCubit ERROR: $e');
        print('Stack trace: $stackTrace');
      }
      final message = e.toString().replaceFirst('Exception: ', '');
      emit(ClientsError(message: message));
    } catch (e, stackTrace) {
      if (kDebugMode) {
        print('ClientsCubit UNEXPECTED ERROR: $e');
        print('Stack trace: $stackTrace');
      }
      emit(ClientsError(message: 'Unexpected error: ${e.toString()}'));
    }
  }

  Future<List<Map<String, dynamic>>> getCountries() async {
    if (_countries.isNotEmpty) {
      return _countries;
    }

    try {
      _countries = await _clientsRepository.getCountries();
      return _countries;
    } on Exception catch (_) {
      return [];
    }
  }
}
