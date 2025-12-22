const String searchCustomersQuery = r'''
  query SearchCustomers($first: Int, $after: String, $searchTerm: String) {
    searchCustomers(first: $first, after: $after, searchTerm: $searchTerm) {
      edges {
        node {
          id
          firstName
          lastName
          email
          phoneNumber
        }
      }
      pageInfo {
        hasNextPage
        endCursor
      }
      totalCount
    }
  }
''';

const String getCustomersQuery = r'''
  query GetCustomers($first: Int, $after: String) {
    customers(first: $first, after: $after) {
      edges {
        node {
          id
          firstName
          lastName
          email
          phoneNumber
        }
      }
      pageInfo {
        hasNextPage
        endCursor
      }
      totalCount
    }
  }
''';

const String createCustomerMutation = r'''
  mutation CreateCustomer($input: CreateCustomerInput!) {
    createCustomer(input: $input) {
      id
      firstName
      lastName
      email
      phoneNumber
    }
  }
''';
