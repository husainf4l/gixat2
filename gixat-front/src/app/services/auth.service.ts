import { Injectable, inject, signal } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map, tap, shareReplay } from 'rxjs';

const LOGIN_MUTATION = gql`
  mutation Login($input: LoginInput!) {
    login(input: $input) {
      user {
        id
        email
        fullName
      }
      error
    }
  }
`;

const REGISTER_MUTATION = gql`
  mutation Register($input: RegisterInput!) {
    register(input: $input) {
      user {
        id
        email
        fullName
      }
      error
    }
  }
`;

const ME_QUERY = gql`
  query Me {
    me {
      id
      email
      fullName
      roles
      organizationId
      organization {
        id
        name
      }
    }
  }
`;

const CREATE_ORGANIZATION_MUTATION = gql`
  mutation CreateOrganization($input: CreateOrganizationInput!) {
    createOrganization(input: $input) {
      user {
        id
        email
        fullName
        organizationId
      }
      error
    }
  }
`;

const INVITE_BY_CODE_QUERY = gql`
  query InviteByCode($code: String!) {
    inviteByCode(code: $code) {
      id
      email
      role
      inviteCode
      status
      organizationId
      organization {
        id
        name
      }
    }
  }
`;

const ASSIGN_USER_TO_ORGANIZATION_MUTATION = gql`
  mutation AssignUserToOrganization($organizationId: UUID!, $userId: String!) {
    assignUserToOrganization(organizationId: $organizationId, userId: $userId)
  }
`;

const LOGIN_WITH_GOOGLE_MUTATION = gql`
  mutation LoginWithGoogle($idToken: String!) {
    loginWithGoogle(idToken: $idToken) {
      success
      token
      message
      user {
        id
        email
      }
    }
  }
`;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apollo = inject(Apollo);
  currentUser = signal<any>(null);

  me(): Observable<any> {
    return this.apollo.query<any>({
      query: ME_QUERY,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data),
      tap(data => this.currentUser.set(data?.me))
    );
  }

  login(input: any): Observable<any> {
    return this.apollo.mutate({
      mutation: LOGIN_MUTATION,
      variables: { input }
    }).pipe(map(result => result.data));
  }

  register(input: any): Observable<any> {
    return this.apollo.mutate({
      mutation: REGISTER_MUTATION,
      variables: { input }
    }).pipe(map(result => result.data));
  }

  createOrganization(input: any): Observable<any> {
    return this.apollo.mutate({
      mutation: CREATE_ORGANIZATION_MUTATION,
      variables: { input }
    }).pipe(
      map(result => result.data),
      tap(() => {
        // Refresh user data after creating organization
        this.me().subscribe();
      })
    );
  }

  validateInviteCode(code: string): Observable<any> {
    return this.apollo.query<any>({
      query: INVITE_BY_CODE_QUERY,
      variables: { code },
      fetchPolicy: 'network-only'
    }).pipe(map(result => result.data));
  }

  joinOrganization(inviteCode: string): Observable<any> {
    // First validate the invite code to get organization details
    return this.validateInviteCode(inviteCode).pipe(
      map(data => {
        if (!data.inviteByCode) {
          throw new Error('Invalid or expired invitation code');
        }
        if (data.inviteByCode.status !== 'PENDING') {
          throw new Error('This invitation has already been used or cancelled');
        }
        return data.inviteByCode;
      }),
      tap(() => {
        // Refresh user data after joining organization
        this.me().subscribe();
      })
    );
  }

  loginWithGoogle(idToken: string): Observable<any> {
    return this.apollo.mutate<{ loginWithGoogle: any }>({
      mutation: LOGIN_WITH_GOOGLE_MUTATION,
      variables: { idToken }
    }).pipe(
      map(result => result.data),
      tap(data => {
        if (data?.loginWithGoogle?.success && data?.loginWithGoogle?.user) {
          this.currentUser.set(data.loginWithGoogle.user);
        }
      })
    );
  }
}