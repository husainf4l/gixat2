import 'package:gql/ast.dart';
import 'package:graphql_flutter/graphql_flutter.dart';

class AutocompleteQueries {
  static final String searchCustomers = r'''
    query SearchCustomers($where: CustomerFilterInput) {
      customers(where: $where) {
        id
        firstName
        lastName
        phoneNumber
        email
      }
    }
  ''';

  static final String searchCars = r'''
    query SearchCars($where: CarFilterInput) {
      cars(where: $where) {
        id
        make
        model
        year
        licensePlate
        vin
        color
        customer {
          id
          firstName
          lastName
        }
      }
    }
  ''';

  static final String getAutocompleteItems = r'''
    query GetAutocompleteItems($category: String!, $query: String) {
      autocompleteItems(category: $category, query: $query) {
        id
        category
        value
        metadata
      }
    }
  ''';
}
