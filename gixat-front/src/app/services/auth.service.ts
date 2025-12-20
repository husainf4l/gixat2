import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map } from 'rxjs';

const LOGIN_MUTATION = gql`
  mutation Login($input: LoginInput!) {
    login(input: $input) {
      token
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
      token
      user {
        id
        email
        fullName
      }
      error
    }
  }
`;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apollo = inject(Apollo);

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
