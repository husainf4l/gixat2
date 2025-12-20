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

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apollo = inject(Apollo);
  currentUser = signal<any>(null);

  me(): Observable<any> {
    return this.apollo.query({
      query: ME_QUERY,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data),
      tap(data => this.currentUser.set(data.me))
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
}
