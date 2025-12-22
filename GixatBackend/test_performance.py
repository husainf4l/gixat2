import requests
import time
import json

# GraphQL endpoint
url = "http://localhost:5000/graphql"

# Login mutation
login_mutation = """
mutation Login {
  login(input: {
    email: "al-hussein@papayatrading.com"
    password: "TT%%oo77"
  }) {
    token
    user {
      id
      email
      fullName
    }
    errors {
      __typename
    }
  }
}
"""

# Login
print("🔐 Logging in...")
response = requests.post(url, json={"query": login_mutation})
login_data = response.json()

if 'errors' in login_data:
    print("❌ Login failed:")
    print(json.dumps(login_data, indent=2))
    exit(1)

token = login_data['data']['login']['token']
user = login_data['data']['login']['user']
print(f"✅ Logged in as: {user['fullName']} ({user['email']})")
print(f"🎫 Token: {token[:50]}...\n")

# Customers query
customers_query = """
query GetCustomers {
  customers(first: 10) {
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
    totalCount
    edges {
      cursor
      node {
        id
        firstName
        lastName
        email
        phoneNumber
        address {
          city
        }
        cars {
          id
        }
        lastSessionDate
        totalVisits
        totalSpent
        activeJobCards
        totalCars
      }
    }
  }
}
"""

# Query customers with timing
print("📊 Querying customers...")
headers = {"Authorization": f"Bearer {token}"}

start_time = time.time()
response = requests.post(url, json={"query": customers_query}, headers=headers)
end_time = time.time()

query_time = (end_time - start_time) * 1000  # Convert to milliseconds

if response.status_code == 200:
    data = response.json()
    if 'errors' in data:
        print("❌ Query failed:")
        print(json.dumps(data, indent=2))
    else:
        customers = data['data']['customers']
        print(f"\n✅ Query successful!")
        print(f"📈 Total customers: {customers['totalCount']}")
        print(f"📋 Returned: {len(customers['edges'])} customers")
        print(f"\n⏱️  Query time: {query_time:.2f}ms")
        
        if query_time < 100:
            print("🚀 EXCELLENT! Under 100ms target!")
        elif query_time < 200:
            print("✨ GOOD! Under 200ms")
        else:
            print("⚠️  Needs optimization - over 200ms")
        
        print("\n📝 Sample customer:")
        if customers['edges']:
            sample = customers['edges'][0]['node']
            print(json.dumps(sample, indent=2))
else:
    print(f"❌ HTTP Error: {response.status_code}")
    print(response.text)
