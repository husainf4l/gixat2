const String getJobCardsQuery = r'''
  query GetJobCards($first: Int, $after: String) {
    jobCards(first: $first, after: $after) {
      edges {
        node {
          id
          jobNumber
          status
          description
          estimatedCost
          createdAt
          car {
            id
            make
            model
            licensePlate
            year
          }
          customer {
            id
            firstName
            lastName
          }
          session {
            id
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

const String createJobCardMutation = r'''
  mutation CreateJobCard($sessionId: UUID!, $description: String, $estimatedCost: Float) {
    createJobCard(sessionId: $sessionId, description: $description, estimatedCost: $estimatedCost) {
      id
      jobNumber
      status
      description
      estimatedCost
      createdAt
    }
  }
''';
