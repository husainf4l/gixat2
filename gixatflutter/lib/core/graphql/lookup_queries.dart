const String getCountriesWithCitiesQuery = r'''
query GetCountriesWithCities {
  lookupItems(where: { category: { eq: "Country" } }) {
    id
    value
    metadata
    children {
      id
      value
    }
  }
}
''';
