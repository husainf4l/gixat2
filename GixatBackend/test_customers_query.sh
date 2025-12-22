#!/bin/bash

# GraphQL query to test customer performance
curl -X POST http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -w "\n\nTime: %{time_total}s\n" \
  -d '{
    "query": "query GetCustomers { customers(first: 10) { pageInfo { hasNextPage hasPreviousPage startCursor endCursor } totalCount edges { cursor node { id firstName lastName email phoneNumber address { city } cars { id } lastSessionDate totalVisits totalSpent activeJobCards totalCars } } } }"
  }'
