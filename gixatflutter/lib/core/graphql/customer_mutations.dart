const String createCustomerMutation = r'''
mutation CreateCustomer($input: CreateCustomerInput!) {
  createCustomer(input: $input) {
    id
    firstName
    lastName
    phoneNumber
    email
    country
    city
    street
  }
}
''';
