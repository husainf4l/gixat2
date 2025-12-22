const String getSessionsQuery = r'''
  query GetSessions($first: Int, $after: String, $where: GarageSessionFilterInput, $order: [GarageSessionSortInput!]) {
    sessions(first: $first, after: $after, where: $where, order: $order) {
      edges {
        node {
          id
          status
          createdAt
          car {
            make
            model
            licensePlate
          }
          customer {
            firstName
            lastName
          }
        }
        cursor
      }
      pageInfo {
        hasNextPage
        hasPreviousPage
        startCursor
        endCursor
      }
      totalCount
    }
  }
''';

const String getSessionByIdQuery = r'''
  query GetSessionById($id: UUID!) {
    sessionById(id: $id) {
      id
      status
      createdAt
      updatedAt
      intakeNotes
      customerRequests
      inspectionNotes
      testDriveNotes
      initialReport
      car {
        id
        make
        model
        year
        licensePlate
        vin
        color
      }
      customer {
        id
        firstName
        lastName
        email
        phoneNumber
      }
      organization {
        id
        name
      }
      media {
        id
        stage
        media {
          id
          url
          alt
          type
        }
      }
      logs {
        id
        fromStatus
        toStatus
        notes
        createdAt
      }
    }
  }
''';

const String createSessionMutation = r'''
  mutation CreateSession($carId: UUID!, $customerId: UUID!) {
    createSession(carId: $carId, customerId: $customerId) {
      id
      status
      createdAt
      car {
        make
        model
        licensePlate
      }
      customer {
        firstName
        lastName
      }
    }
  }
''';

const String updateSessionStatusMutation = r'''
  mutation UpdateSessionStatus($id: UUID!, $status: SessionStatus!) {
    updateSessionStatus(id: $id, status: $status) {
      id
      status
      updatedAt
    }
  }
''';
