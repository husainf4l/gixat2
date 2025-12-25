import { Injectable } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';

export interface AuthPayload {
  token?: string;
  user?: ApplicationUser;
  error?: string;
}

export interface ApplicationUser {
  id: string;
  fullName?: string;
  email?: string;
  userType: string;
  organizationId?: string;
  organization?: any;
  roles: string[];
  avatarS3Key?: string;
  bio?: string;
  createdAt: string;
  isActive: boolean;
}

export interface LoginInput {
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'user_data';

  private currentUserSubject = new BehaviorSubject<ApplicationUser | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private apollo: Apollo) {}

  get currentUser(): ApplicationUser | null {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return !!this.currentUser && !!this.getToken();
  }

  login(credentials: LoginInput): Observable<AuthPayload> {
    const LOGIN_MUTATION = gql`
      mutation Login($input: LoginInput!) {
        login(input: $input) {
          token
          user {
            id
            fullName
            email
            userType
            organizationId
            organization {
              id
              name
            }
            roles
            avatarS3Key
            bio
            createdAt
            isActive
          }
          error
        }
      }
    `;

    return this.apollo.mutate<{ login: AuthPayload }>({
      mutation: LOGIN_MUTATION,
      variables: { input: credentials }
    }).pipe(
      map(result => result.data?.login || { error: 'Login failed' }),
      tap(payload => {
        if (payload.token && payload.user) {
          this.setToken(payload.token);
          this.setUser(payload.user);
          this.currentUserSubject.next(payload.user);
        }
      })
    );
  }

  logout(): void {
    // Clear Apollo cache
    this.apollo.client.resetStore();

    // Clear local storage
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);

    // Update current user
    this.currentUserSubject.next(null);
  }

  private setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  private getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private setUser(user: ApplicationUser): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private getUserFromStorage(): ApplicationUser | null {
    const userData = localStorage.getItem(this.USER_KEY);
    return userData ? JSON.parse(userData) : null;
  }
}