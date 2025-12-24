import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

// Enums
export enum InviteStatus {
  PENDING = 'PENDING',
  ACCEPTED = 'ACCEPTED',
  EXPIRED = 'EXPIRED',
  CANCELED = 'CANCELED'
}

// Interfaces
export interface UserInvite {
  id: string;
  email: string;
  role: string;
  inviteCode: string;
  expiryDate: string;
  status: InviteStatus;
  createdAt: string;
  organizationId: string;
  inviter?: {
    id: string;
    fullName: string;
    email: string;
  };
  organization?: {
    id: string;
    name: string;
  };
}

export interface InviteUserInput {
  email: string;
  role: string;
}

export interface InvitePayload {
  invite: UserInvite | null;
  link: string | null;
  error: string | null;
}

// GraphQL Queries
const INVITE_USER = gql`
  mutation InviteUser($input: InviteUserInput!) {
    inviteUser(input: $input) {
      invite {
        id
        email
        role
        inviteCode
        expiryDate
        status
        createdAt
        organizationId
        inviter {
          id
          fullName
          email
        }
      }
      link
      error
    }
  }
`;

const GET_INVITES = gql`
  query GetInvites(
    $where: UserInviteFilterInput
    $order: [UserInviteSortInput!]
  ) {
    invites(where: $where, order: $order) {
      id
      email
      role
      inviteCode
      expiryDate
      status
      createdAt
      organizationId
      inviter {
        id
        fullName
        email
      }
    }
  }
`;

const GET_INVITE_BY_CODE = gql`
  query GetInviteByCode($code: String!) {
    inviteByCode(code: $code) {
      id
      email
      role
      inviteCode
      expiryDate
      status
      organizationId
      organization {
        id
        name
      }
    }
  }
`;

const CANCEL_INVITE = gql`
  mutation CancelInvite($id: UUID!) {
    cancelInvite(id: $id)
  }
`;

@Injectable({
  providedIn: 'root'
})
export class InviteService {
  private apollo = inject(Apollo);

  /**
   * Create a new user invite
   */
  inviteUser(input: InviteUserInput): Observable<InvitePayload> {
    return this.apollo.mutate<{ inviteUser: InvitePayload }>({
      mutation: INVITE_USER,
      variables: { input }
    }).pipe(
      map(result => result.data?.inviteUser!)
    );
  }

  /**
   * Get all invites with optional filtering and sorting
   */
  getInvites(where?: any, order?: any): Observable<UserInvite[]> {
    return this.apollo.query<{ invites: UserInvite[] }>({
      query: GET_INVITES,
      variables: { where, order },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.invites!)
    );
  }

  /**
   * Get invite by code (public endpoint)
   */
  getInviteByCode(code: string): Observable<UserInvite | null> {
    return this.apollo.query<{ inviteByCode: UserInvite | null }>({
      query: GET_INVITE_BY_CODE,
      variables: { code },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.inviteByCode ?? null)
    );
  }

  /**
   * Cancel a pending invite
   */
  cancelInvite(id: string): Observable<boolean> {
    return this.apollo.mutate<{ cancelInvite: boolean }>({
      mutation: CANCEL_INVITE,
      variables: { id }
    }).pipe(
      map(result => result.data?.cancelInvite ?? false)
    );
  }

  /**
   * Helper: Get pending invites only
   */
  getPendingInvites(): Observable<UserInvite[]> {
    return this.getInvites(
      { status: { eq: 'PENDING' } },
      [{ createdAt: 'DESC' }]
    );
  }

  /**
   * Helper: Check if invite is expired
   */
  isExpired(invite: UserInvite): boolean {
    return new Date(invite.expiryDate) < new Date();
  }

  /**
   * Helper: Format expiry date for display
   */
  getExpiryText(invite: UserInvite): string {
    const expiry = new Date(invite.expiryDate);
    const now = new Date();
    const diffMs = expiry.getTime() - now.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffMins = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

    if (diffMs < 0) {
      return 'Expired';
    } else if (diffHours > 0) {
      return `Expires in ${diffHours}h ${diffMins}m`;
    } else if (diffMins > 0) {
      return `Expires in ${diffMins}m`;
    } else {
      return 'Expiring soon';
    }
  }

  /**
   * Helper: Get status badge color
   */
  getStatusColor(status: InviteStatus): string {
    switch (status) {
      case InviteStatus.PENDING:
        return 'bg-amber-100 text-amber-700 border-amber-200';
      case InviteStatus.ACCEPTED:
        return 'bg-emerald-100 text-emerald-700 border-emerald-200';
      case InviteStatus.EXPIRED:
        return 'bg-slate-100 text-slate-600 border-slate-200';
      case InviteStatus.CANCELED:
        return 'bg-red-100 text-red-700 border-red-200';
      default:
        return 'bg-slate-100 text-slate-600 border-slate-200';
    }
  }

  /**
   * Helper: Get status icon
   */
  getStatusIcon(status: InviteStatus): string {
    switch (status) {
      case InviteStatus.PENDING:
        return 'ri-time-line';
      case InviteStatus.ACCEPTED:
        return 'ri-checkbox-circle-line';
      case InviteStatus.EXPIRED:
        return 'ri-close-circle-line';
      case InviteStatus.CANCELED:
        return 'ri-forbid-line';
      default:
        return 'ri-question-line';
    }
  }
}
