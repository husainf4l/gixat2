# Testing GraphQL Endpoint

## GraphQL Endpoint
https://gixat.com/graphql/

## Testing Steps

### 1. Test Introspection Query
Run this query to see the actual schema:

```graphql
query IntrospectionQuery {
  __schema {
    queryType {
      name
      fields {
        name
        type {
          name
          kind
        }
      }
    }
    mutationType {
      name
      fields {
        name
        type {
          name
          kind
        }
      }
    }
  }
}
```

### 2. Test Login Mutation
Try this mutation (may need to adjust field names based on introspection):

```graphql
mutation Login {
  tokenAuth(email: "test@example.com", password: "password123") {
    token
    refreshToken
    user {
      id
      email
      firstName
      lastName
    }
  }
}
```

### 3. Test Register Mutation
```graphql
mutation Register {
  createUser(
    email: "newuser@example.com"
    password: "password123"
    firstName: "John"
    lastName: "Doe"
  ) {
    user {
      id
      email
      firstName
      lastName
    }
    token
  }
}
```

### 4. Test Dashboard Query
```graphql
query Dashboard {
  dashboardStats {
    todaySessions
    activeJobCards
    pendingAppointments
    carsInGarage
  }
  todayAppointments {
    id
    time
    client
    vehicle
    status
  }
  activeJobCards {
    id
    jobNumber
    client
    vehicle
    status
    assignedMechanic
  }
  alerts {
    type
    message
    severity
    actionRequired
  }
}
```

## Using Postman or cURL

### cURL Example for Introspection:
```bash
curl -X POST https://gixat.com/graphql/ \
  -H "Content-Type: application/json" \
  -d '{
    "query": "{ __schema { queryType { name fields { name } } } }"
  }'
```

### cURL Example for Login:
```bash
curl -X POST https://gixat.com/graphql/ \
  -H "Content-Type: application/json" \
  -d '{
    "query": "mutation { tokenAuth(email: \"test@example.com\", password: \"password123\") { token user { id email } } }"
  }'
```

## Next Steps After Testing

1. If field names are different, update:
   - `/lib/core/graphql/auth_queries.dart`
   - `/lib/core/graphql/dashboard_queries.dart`

2. If schema uses different mutation/query names, update them accordingly

3. Test with real credentials from the Gixat backend

4. Verify token format and authentication flow
