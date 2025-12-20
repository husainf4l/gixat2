class AutocompleteQueries {
  static const String searchCustomers = r'''
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

  static const String searchCars = r'''
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

  static const String getAutocompleteItems = r'''
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
