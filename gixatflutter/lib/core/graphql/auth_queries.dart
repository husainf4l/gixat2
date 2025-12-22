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

const String registerWithOrgMutation = r'''
  mutation RegisterWithOrg(
    $email: String!
    $password: String!
    $fullName: String!
    $role: String!
    $userType: UserType!
    $organizationId: UUID!
  ) {
    register(
      input: {
        email: $email
        password: $password
        fullName: $fullName
        role: $role
        userType: $userType
        organizationId: $organizationId
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
      organizationId
      organization {
        id
        name
      }
    }
  }
''';

const String myOrganizationQuery = r'''
  query MyOrganization {
    myOrganization {
      id
      name
    }
  }
''';

const String createOrganizationMutation = r'''
  mutation CreateOrganization($input: CreateOrganizationInput!) {
    createOrganization(input: $input) {
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

const String signupWithOrganizationMutation = r'''
  mutation SignupWithOrg(
    $email: String!
    $password: String!
    $fullName: String!
    $name: String!
    $country: String!
    $city: String!
    $street: String!
    $phoneCountryCode: String!
  ) {
    createOrganization(
      input: {
        name: $name
        address: {
          country: $country
          city: $city
          street: $street
          phoneCountryCode: $phoneCountryCode
        }
        email: $email
        password: $password
        fullName: $fullName
        role: "OWNER"
        userType: ORGANIZATIONAL
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
