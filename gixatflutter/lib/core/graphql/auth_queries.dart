const String loginMutation = r'''
  mutation Login($email: String!, $password: String!) {
    login(input: { email: $email, password: $password }) {
      token
      error
      user {
        id
        email
        fullName
      }
    }
  }
''';

const String registerMutation = r'''
  mutation Register(
    $email: String!
    $password: String!
    $fullName: String!
    $role: String!
    $userType: UserType!
  ) {
    register(
      input: {
        email: $email
        password: $password
        fullName: $fullName
        role: $role
        userType: $userType
      }
    ) {
      token
      error
      user {
        id
        email
        fullName
      }
    }
  }
''';

const String verifyTokenQuery = r'''
  query VerifyToken($token: String!) {
    verifyToken(token: $token) {
      payload
    }
  }
''';

const String meQuery = r'''
  query Me {
    me {
      id
      email
      fullName
    }
  }
''';

const String createOrganizationMutation = r'''
  mutation CreateOrganization($input: CreateOrganizationInput!) {
    createOrganization(input: $input) {
      id
      name
      address {
        id
        country
        city
        street
        phoneCountryCode
      }
    }
  }
''';
