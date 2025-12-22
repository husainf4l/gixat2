const String getCarsQuery = r'''
  query GetCars($first: Int, $after: String, $where: CarFilterInput) {
    cars(first: $first, after: $after, where: $where) {
      edges {
        node {
          id
          make
          model
          year
          licensePlate
          vin
          color
          customerId
          customer {
            firstName
            lastName
          }
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

const String createCarMutation = r'''
  mutation CreateCar($input: CreateCarInput!) {
    createCar(input: $input) {
      id
      make
      model
      year
      licensePlate
      vin
      color
      customerId
    }
  }
''';
