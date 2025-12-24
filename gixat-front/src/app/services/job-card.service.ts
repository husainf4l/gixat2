import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { SessionCar, SessionCustomer, AppMedia } from './session.service';

export enum JobCardStatus {
  PENDING = 'PENDING',
  IN_PROGRESS = 'IN_PROGRESS',
  COMPLETED = 'COMPLETED',
  CANCELLED = 'CANCELLED'
}

export enum JobItemStatus {
  PENDING = 'PENDING',
  IN_PROGRESS = 'IN_PROGRESS',
  COMPLETED = 'COMPLETED',
  CANCELLED = 'CANCELLED'
}

export enum JobCardMediaType {
  BEFORE_WORK = 'BEFORE_WORK',
  DURING_WORK = 'DURING_WORK',
  AFTER_WORK = 'AFTER_WORK',
  DOCUMENTATION = 'DOCUMENTATION'
}

export interface Technician {
  id: string;
  fullName: string;
  email: string;
}

export interface JobCardMedia {
  id: string;
  type: JobCardMediaType;
  media: AppMedia;
  createdAt: string;
}

export interface JobItem {
  id: string;
  description: string;
  estimatedLaborCost: number;
  estimatedPartsCost: number;
  actualLaborCost: number;
  actualPartsCost: number;
  status: JobItemStatus;
  isApprovedByCustomer: boolean;
  approvedAt?: string | null;
  technicianNotes?: string | null;
  assignedTechnicianId?: string | null;
  assignedTechnician?: Technician | null;
  media: JobCardMedia[];
  comments: JobCardComment[];
  createdAt: string;
  updatedAt: string;
}

export interface ApplicationUser {
  id: string;
  fullName: string;
  email: string;
  avatar?: string | null;
  userName?: string | null;
}

export interface JobCardComment {
  id: string;
  jobCardId: string;
  jobItemId?: string | null;
  authorId: string;
  author: ApplicationUser;
  content: string;
  parentCommentId?: string | null;
  parentComment?: JobCardComment | null;
  replies: JobCardComment[];
  mentions: JobCardCommentMention[];
  isEdited: boolean;
  editedAt?: string | null;
  isDeleted: boolean;
  deletedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface JobCardCommentMention {
  id: string;
  commentId: string;
  comment: JobCardComment;
  mentionedUserId: string;
  mentionedUser: ApplicationUser;
  isRead: boolean;
  readAt?: string | null;
  createdAt: string;
}

export interface CommentsConnection {
  edges: {
    node: JobCardComment;
    cursor: string;
  }[];
  pageInfo: {
    hasNextPage: boolean;
    hasPreviousPage: boolean;
    endCursor: string | null;
  };
}

export interface JobCard {
  id: string;
  status: JobCardStatus;
  totalEstimatedCost: number;
  totalActualCost: number;
  totalEstimatedLabor: number;
  totalActualLabor: number;
  totalEstimatedParts: number;
  totalActualParts: number;
  isApprovedByCustomer: boolean;
  approvedAt?: string | null;
  internalNotes?: string | null;
  assignedTechnicianId?: string | null;
  assignedTechnician?: Technician | null;
  createdAt: string;
  updatedAt: string;
  carId: string;
  customerId: string;
  sessionId?: string | null;
  car: SessionCar | null;
  customer: SessionCustomer | null;
  items: JobItem[];
  media: JobCardMedia[];
  comments: JobCardComment[];
}

export interface JobCardsConnection {
  edges: {
    node: JobCard;
    cursor: string;
  }[];
  pageInfo: {
    hasNextPage: boolean;
    hasPreviousPage: boolean;
    startCursor: string | null;
    endCursor: string | null;
  };
  totalCount: number;
}

const GET_JOB_CARDS_QUERY = gql`
  query GetJobCards($first: Int, $after: String, $where: JobCardFilterInput, $order: [JobCardSortInput!]) {
    jobCards(first: $first, after: $after, where: $where, order: $order) {
      edges {
        node {
          id
          status
          totalEstimatedCost
          totalActualCost
          totalEstimatedLabor
          totalEstimatedParts
          isApprovedByCustomer
          createdAt
          car {
            make
            model
            licensePlate
          }
          customer {
            firstName
            lastName
          }
          assignedTechnician {
            fullName
          }
        }
        cursor
      }
      pageInfo {
        hasNextPage
        hasPreviousPage
        startCursor
        endCursor
      }
      totalCount
    }
  }
`;

const SEARCH_JOB_CARDS_QUERY = gql`
  query SearchJobCards($query: String, $status: JobCardStatus, $first: Int, $after: String) {
    searchJobCards(query: $query, status: $status, first: $first, after: $after) {
      edges {
        node {
          id
          status
          totalEstimatedCost
          totalActualCost
          isApprovedByCustomer
          createdAt
          car {
            make
            model
            licensePlate
          }
          customer {
            firstName
            lastName
          }
        }
        cursor
      }
      pageInfo {
        hasNextPage
        endCursor
      }
    }
  }
`;

const GET_JOB_CARD_BY_ID_QUERY = gql`
  query GetJobCardById($id: UUID!) {
    jobCardById(id: $id) {
      id
      status
      totalEstimatedCost
      totalActualCost
      totalEstimatedLabor
      totalActualLabor
      totalEstimatedParts
      totalActualParts
      isApprovedByCustomer
      approvedAt
      internalNotes
      createdAt
      updatedAt
      assignedTechnician {
        id
        fullName
        email
      }
      car {
        id
        make
        model
        year
        licensePlate
        color
        vin
      }
      customer {
        id
        firstName
        lastName
        email
        phoneNumber
      }
      items {
        id
        description
        estimatedLaborCost
        estimatedPartsCost
        actualLaborCost
        actualPartsCost
        status
        isApprovedByCustomer
        approvedAt
        technicianNotes
        assignedTechnician {
          id
          fullName
        }
        media {
          id
          type
          media {
            id
            url
            alt
            type
          }
        }
        createdAt
        updatedAt
      }
      media {
        id
        type
        media {
          id
          url
          alt
          type
        }
      }
    }
  }
`;

const ADD_JOB_ITEM_MUTATION = gql`
  mutation AddJobItem($jobCardId: UUID!, $description: String!, $estimatedLaborCost: Decimal!, $estimatedPartsCost: Decimal!, $assignedTechnicianId: String) {
    addJobItem(jobCardId: $jobCardId, description: $description, estimatedLaborCost: $estimatedLaborCost, estimatedPartsCost: $estimatedPartsCost, assignedTechnicianId: $assignedTechnicianId) {
      id
      totalEstimatedLabor
      totalEstimatedParts
      totalEstimatedCost
    }
  }
`;

const UPDATE_JOB_ITEM_STATUS_MUTATION = gql`
  mutation UpdateJobItemStatus($itemId: UUID!, $status: JobItemStatus!, $actualLaborCost: Decimal!, $actualPartsCost: Decimal!, $technicianNotes: String) {
    updateJobItemStatus(itemId: $itemId, status: $status, actualLaborCost: $actualLaborCost, actualPartsCost: $actualPartsCost, technicianNotes: $technicianNotes) {
      id
      status
      actualLaborCost
      actualPartsCost
      technicianNotes
    }
  }
`;

const UPDATE_JOB_CARD_STATUS_MUTATION = gql`
  mutation UpdateJobCardStatus($jobCardId: UUID!, $status: JobCardStatus!) {
    updateJobCardStatus(jobCardId: $jobCardId, status: $status) {
      id
      status
    }
  }
`;

const ASSIGN_TECHNICIAN_TO_JOB_CARD_MUTATION = gql`
  mutation AssignTechnicianToJobCard($jobCardId: UUID!, $technicianId: String!) {
    assignTechnicianToJobCard(jobCardId: $jobCardId, technicianId: $technicianId) {
      id
      assignedTechnician {
        id
        fullName
      }
    }
  }
`;

const ASSIGN_TECHNICIAN_TO_JOB_ITEM_MUTATION = gql`
  mutation AssignTechnicianToJobItem($itemId: UUID!, $technicianId: String!) {
    assignTechnicianToJobItem(itemId: $itemId, technicianId: $technicianId) {
      id
      assignedTechnician {
        id
        fullName
      }
    }
  }
`;

const APPROVE_JOB_CARD_MUTATION = gql`
  mutation ApproveJobCard($jobCardId: UUID!) {
    approveJobCard(jobCardId: $jobCardId) {
      id
      isApprovedByCustomer
      approvedAt
    }
  }
`;

const UPLOAD_MEDIA_TO_JOB_ITEM_MUTATION = gql`
  mutation UploadMediaToJobItem($itemId: UUID!, $file: Upload!, $type: JobCardMediaType!, $alt: String) {
    uploadMediaToJobItem(itemId: $itemId, file: $file, type: $type, alt: $alt) {
      id
      media {
        id
        url
        alt
      }
    }
  }
`;

const CREATE_JOB_CARD_FROM_SESSION_MUTATION = gql`
  mutation CreateJobCardFromSession($sessionId: UUID!) {
    createJobCardFromSession(sessionId: $sessionId) {
      id
      status
      items {
        id
        description
      }
    }
  }
`;

const APPROVE_JOB_ITEM_MUTATION = gql`
  mutation ApproveJobItem($itemId: UUID!) {
    approveJobItem(itemId: $itemId) {
      id
      isApprovedByCustomer
      approvedAt
    }
  }
`;

const UPLOAD_MEDIA_TO_JOB_CARD_MUTATION = gql`
  mutation UploadMediaToJobCard($jobCardId: UUID!, $file: Upload!, $type: JobCardMediaType!, $alt: String) {
    uploadMediaToJobCard(jobCardId: $jobCardId, file: $file, type: $type, alt: $alt) {
      id
      media {
        id
        url
        alt
      }
      type
    }
  }
`;

const GET_JOB_CARDS_BY_CUSTOMER_QUERY = gql`
  query GetJobCardsByCustomer($customerId: UUID!) {
    jobCardsByCustomer(customerId: $customerId) {
      nodes {
        id
        status
        car {
          make
          model
        }
        totalEstimatedCost
        createdAt
      }
    }
  }
`;

const GET_JOB_CARDS_BY_STATUS_QUERY = gql`
  query GetJobCardsByStatus($status: JobCardStatus!) {
    jobCardsByStatus(status: $status) {
      nodes {
        id
        car {
          make
          model
          licensePlate
        }
        customer {
          firstName
          lastName
        }
        createdAt
      }
    }
  }
`;

// ============================================================================
// ESTIMATE APPROVAL QUERIES & MUTATIONS
// ============================================================================

const GET_ESTIMATE_BY_SHARE_TOKEN_QUERY = gql`
  query GetEstimateByShareToken($shareToken: String!) {
    estimateByShareToken(token: $shareToken) {
      id
      jobCardId
      shareToken
      expiresAt
      isActive
      jobCard {
        id
        status
        totalEstimatedCost
        totalEstimatedLabor
        totalEstimatedParts
        isApprovedByCustomer
        approvedAt
        createdAt
        customer {
          id
          firstName
          lastName
          email
          phoneNumber
        }
        car {
          id
          make
          model
          year
          licensePlate
          color
        }
        items {
          id
          description
          estimatedLaborCost
          estimatedPartsCost
          isApprovedByCustomer
          approvedAt
          technicianNotes
          status
        }
        assignedTechnician {
          id
          fullName
          email
        }
        organization {
          id
          name
          logo {
            url
          }
        }
      }
    }
  }
`;

const GENERATE_ESTIMATE_SHARE_LINK_MUTATION = gql`
  mutation GenerateEstimateShareLink($jobCardId: UUID!, $expiresInHours: Int) {
    generateEstimateShareLink(jobCardId: $jobCardId, expiresInHours: $expiresInHours) {
      id
      shareToken
      shareUrl
      expiresAt
      isActive
    }
  }
`;

const REVOKE_ESTIMATE_SHARE_LINK_MUTATION = gql`
  mutation RevokeEstimateShareLink($shareToken: String!) {
    revokeEstimateShareLink(shareToken: $shareToken) {
      id
      isActive
    }
  }
`;

// ============================================================================
// CHAT/COLLABORATION QUERIES
// ============================================================================

const GET_JOB_CARD_COMMENTS_QUERY = gql`
  query GetJobCardComments($jobCardId: UUID!, $first: Int) {
    jobCardComments(jobCardId: $jobCardId, first: $first) {
      pageInfo {
        hasNextPage
        endCursor
      }
      edges {
        node {
          id
          content
          author {
            id
            fullName
            email
            avatar
            userName
          }
          jobItemId
          parentCommentId
          replies {
            id
            content
            author {
              id
              fullName
              avatar
            }
            createdAt
          }
          mentions {
            id
            mentionedUser {
              id
              fullName
              avatar
            }
            isRead
            readAt
          }
          isEdited
          editedAt
          isDeleted
          deletedAt
          createdAt
          updatedAt
        }
        cursor
      }
    }
  }
`;

const GET_JOB_ITEM_COMMENTS_QUERY = gql`
  query GetJobItemComments($jobItemId: UUID!) {
    jobItemComments(jobItemId: $jobItemId) {
      nodes {
        id
        content
        author {
          id
          fullName
          avatar
        }
        createdAt
        isEdited
        editedAt
      }
    }
  }
`;

const GET_MY_UNREAD_MENTIONS_QUERY = gql`
  query GetMyUnreadMentions {
    myUnreadMentions {
      nodes {
        id
        comment {
          id
          content
          author {
            id
            fullName
            avatar
          }
          jobCard {
            id
            car {
              make
              model
              licensePlate
            }
          }
          jobItem {
            id
            description
          }
          createdAt
        }
        isRead
        createdAt
      }
    }
  }
`;

const GET_UNREAD_MENTION_COUNT_QUERY = gql`
  query GetUnreadMentionCount {
    unreadMentionCount
  }
`;

const GET_RECENT_JOB_CARD_ACTIVITY_QUERY = gql`
  query GetRecentJobCardActivity($organizationId: UUID!, $first: Int) {
    recentJobCardActivity(organizationId: $organizationId, first: $first) {
      nodes {
        id
        content
        author {
          id
          fullName
          avatar
        }
        jobCard {
          id
          car {
            make
            model
          }
          customer {
            firstName
            lastName
          }
        }
        createdAt
      }
    }
  }
`;

// ============================================================================
// CHAT/COLLABORATION MUTATIONS
// ============================================================================

const ADD_JOB_CARD_COMMENT_MUTATION = gql`
  mutation AddJobCardComment($jobCardId: UUID!, $content: String!, $jobItemId: UUID, $parentCommentId: UUID) {
    addJobCardComment(jobCardId: $jobCardId, content: $content, jobItemId: $jobItemId, parentCommentId: $parentCommentId) {
      id
      content
      author {
        id
        fullName
        avatar
      }
      mentions {
        id
        mentionedUser {
          id
          fullName
        }
      }
      createdAt
    }
  }
`;

const EDIT_JOB_CARD_COMMENT_MUTATION = gql`
  mutation EditJobCardComment($commentId: UUID!, $content: String!) {
    editJobCardComment(commentId: $commentId, content: $content) {
      id
      content
      isEdited
      editedAt
      mentions {
        id
        mentionedUser {
          id
          fullName
        }
      }
    }
  }
`;

const DELETE_JOB_CARD_COMMENT_MUTATION = gql`
  mutation DeleteJobCardComment($commentId: UUID!) {
    deleteJobCardComment(commentId: $commentId) {
      id
      isDeleted
      deletedAt
    }
  }
`;

const MARK_MENTIONS_AS_READ_MUTATION = gql`
  mutation MarkMentionsAsRead($mentionIds: [UUID!]!) {
    markMentionsAsRead(mentionIds: $mentionIds)
  }
`;

@Injectable({ providedIn: 'root' })
export class JobCardService {
  private apollo = inject(Apollo);

  getJobCards(
    first: number = 20,
    after?: string | null,
    where?: any,
    order?: any[]
  ): Observable<JobCardsConnection> {
    return this.apollo.query<{ jobCards: JobCardsConnection }>({
      query: GET_JOB_CARDS_QUERY,
      variables: { 
        first, 
        after,
        where,
        order: order || [{ createdAt: 'DESC' }]
      },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to load job cards');
        return data.jobCards;
      })
    );
  }

  searchJobCards(
    query?: string,
    status?: JobCardStatus,
    first: number = 20,
    after?: string | null
  ): Observable<JobCardsConnection> {
    return this.apollo.query<{ searchJobCards: JobCardsConnection }>({
      query: SEARCH_JOB_CARDS_QUERY,
      variables: { query, status, first, after },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to search job cards');
        return data.searchJobCards;
      })
    );
  }

  getJobCardById(id: string): Observable<JobCard> {
    return this.apollo.query<{ jobCardById: JobCard }>({
      query: GET_JOB_CARD_BY_ID_QUERY,
      variables: { id },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Job card not found');
        return data.jobCardById;
      })
    );
  }

  addJobItem(jobCardId: string, description: string, estimatedLaborCost: number, estimatedPartsCost: number, assignedTechnicianId?: string): Observable<JobCard> {
    return this.apollo.mutate<{ addJobItem: JobCard }>({
      mutation: ADD_JOB_ITEM_MUTATION,
      variables: { jobCardId, description, estimatedLaborCost, estimatedPartsCost, assignedTechnicianId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to add job item');
        return result.data.addJobItem;
      })
    );
  }

  updateJobItemStatus(itemId: string, status: JobItemStatus, actualLaborCost: number, actualPartsCost: number, technicianNotes?: string): Observable<JobItem> {
    return this.apollo.mutate<{ updateJobItemStatus: JobItem }>({
      mutation: UPDATE_JOB_ITEM_STATUS_MUTATION,
      variables: { itemId, status, actualLaborCost, actualPartsCost, technicianNotes },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to update job item status');
        return result.data.updateJobItemStatus;
      })
    );
  }

  updateJobCardStatus(jobCardId: string, status: JobCardStatus): Observable<JobCard> {
    return this.apollo.mutate<{ updateJobCardStatus: JobCard }>({
      mutation: UPDATE_JOB_CARD_STATUS_MUTATION,
      variables: { jobCardId, status },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to update job card status');
        return result.data.updateJobCardStatus;
      })
    );
  }

  assignTechnicianToJobCard(jobCardId: string, technicianId: string): Observable<JobCard> {
    return this.apollo.mutate<{ assignTechnicianToJobCard: JobCard }>({
      mutation: ASSIGN_TECHNICIAN_TO_JOB_CARD_MUTATION,
      variables: { jobCardId, technicianId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to assign technician');
        return result.data.assignTechnicianToJobCard;
      })
    );
  }

  assignTechnicianToJobItem(itemId: string, technicianId: string): Observable<JobItem> {
    return this.apollo.mutate<{ assignTechnicianToJobItem: JobItem }>({
      mutation: ASSIGN_TECHNICIAN_TO_JOB_ITEM_MUTATION,
      variables: { itemId, technicianId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to assign technician to item');
        return result.data.assignTechnicianToJobItem;
      })
    );
  }

  approveJobCard(jobCardId: string): Observable<JobCard> {
    return this.apollo.mutate<{ approveJobCard: JobCard }>({
      mutation: APPROVE_JOB_CARD_MUTATION,
      variables: { jobCardId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to approve job card');
        return result.data.approveJobCard;
      })
    );
  }

  uploadMediaToJobItem(itemId: string, file: File, type: JobCardMediaType, alt?: string): Observable<any> {
    return this.apollo.mutate<{ uploadMediaToJobItem: { id: string; media: { url: string } } }>({
      mutation: UPLOAD_MEDIA_TO_JOB_ITEM_MUTATION,
      variables: { itemId, file, type, alt },
      context: {
        useMultipart: true
      }
    }).pipe(
      map(result => {
        if (!result.data?.uploadMediaToJobItem) {
          throw new Error('Failed to upload media to job item');
        }
        return result.data.uploadMediaToJobItem;
      }),
    );
  }

  createJobCardFromSession(sessionId: string): Observable<JobCard> {
    return this.apollo.mutate<{ createJobCardFromSession: JobCard }>({
      mutation: CREATE_JOB_CARD_FROM_SESSION_MUTATION,
      variables: { sessionId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to create job card from session');
        return result.data.createJobCardFromSession;
      })
    );
  }

  approveJobItem(itemId: string): Observable<JobItem> {
    return this.apollo.mutate<{ approveJobItem: JobItem }>({
      mutation: APPROVE_JOB_ITEM_MUTATION,
      variables: { itemId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to approve job item');
        return result.data.approveJobItem;
      })
    );
  }

  uploadMediaToJobCard(jobCardId: string, file: File, type: JobCardMediaType, alt?: string): Observable<any> {
    return this.apollo.mutate<{ uploadMediaToJobCard: { id: string; media: { url: string }; type: JobCardMediaType } }>({
      mutation: UPLOAD_MEDIA_TO_JOB_CARD_MUTATION,
      variables: { jobCardId, file, type, alt },
      context: {
        useMultipart: true
      }
    }).pipe(
      map(result => {
        if (!result.data?.uploadMediaToJobCard) {
          throw new Error('Failed to upload media to job card');
        }
        return result.data.uploadMediaToJobCard;
      }),
    );
  }

  getJobCardsByCustomer(customerId: string): Observable<JobCard[]> {
    return this.apollo.query<{ jobCardsByCustomer: { nodes: JobCard[] } }>({
      query: GET_JOB_CARDS_BY_CUSTOMER_QUERY,
      variables: { customerId },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to load job cards by customer');
        return data.jobCardsByCustomer.nodes;
      })
    );
  }

  getJobCardsByStatus(status: JobCardStatus): Observable<JobCard[]> {
    return this.apollo.query<{ jobCardsByStatus: { nodes: JobCard[] } }>({
      query: GET_JOB_CARDS_BY_STATUS_QUERY,
      variables: { status },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to load job cards by status');
        return data.jobCardsByStatus.nodes;
      })
    );
  }

  // Business rule validation helpers
  canStartJobItem(item: JobItem): boolean {
    return item.isApprovedByCustomer;
  }

  canCompleteJobItem(item: JobItem): boolean {
    return (item.actualLaborCost > 0 || item.actualPartsCost > 0);
  }

  canCompleteJobCard(jobCard: JobCard): boolean {
    return jobCard.items.every(
      item => item.status === JobItemStatus.COMPLETED || 
              item.status === JobItemStatus.CANCELLED
    );
  }

  calculateJobCardTotals(items: JobItem[]): {
    totalEstimatedCost: number;
    totalActualCost: number;
    totalEstimatedLabor: number;
    totalActualLabor: number;
    totalEstimatedParts: number;
    totalActualParts: number;
  } {
    return items.reduce((acc, item) => ({
      totalEstimatedCost: acc.totalEstimatedCost + (item.estimatedLaborCost + item.estimatedPartsCost),
      totalActualCost: acc.totalActualCost + (item.actualLaborCost + item.actualPartsCost),
      totalEstimatedLabor: acc.totalEstimatedLabor + item.estimatedLaborCost,
      totalActualLabor: acc.totalActualLabor + item.actualLaborCost,
      totalEstimatedParts: acc.totalEstimatedParts + item.estimatedPartsCost,
      totalActualParts: acc.totalActualParts + item.actualPartsCost,
    }), {
      totalEstimatedCost: 0,
      totalActualCost: 0,
      totalEstimatedLabor: 0,
      totalActualLabor: 0,
      totalEstimatedParts: 0,
      totalActualParts: 0,
    });
  }

  // ============================================================================
  // CHAT/COLLABORATION METHODS
  // ============================================================================

  getJobCardComments(jobCardId: string, first: number = 50): Observable<CommentsConnection> {
    return this.apollo.query<{ jobCardComments: CommentsConnection }>({
      query: GET_JOB_CARD_COMMENTS_QUERY,
      variables: { jobCardId, first },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to load comments');
        return data.jobCardComments;
      })
    );
  }

  getJobItemComments(jobItemId: string): Observable<JobCardComment[]> {
    return this.apollo.query<{ jobItemComments: { nodes: JobCardComment[] } }>({
      query: GET_JOB_ITEM_COMMENTS_QUERY,
      variables: { jobItemId },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to load item comments');
        return data.jobItemComments.nodes;
      })
    );
  }

  getMyUnreadMentions(): Observable<JobCardCommentMention[]> {
    return this.apollo.query<{ myUnreadMentions: { nodes: JobCardCommentMention[] } }>({
      query: GET_MY_UNREAD_MENTIONS_QUERY,
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to load unread mentions');
        return data.myUnreadMentions.nodes;
      })
    );
  }

  getUnreadMentionCount(): Observable<number> {
    return this.apollo.query<{ unreadMentionCount: number }>({
      query: GET_UNREAD_MENTION_COUNT_QUERY,
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) return 0;
        return data.unreadMentionCount;
      })
    );
  }

  getRecentJobCardActivity(organizationId: string, first: number = 20): Observable<JobCardComment[]> {
    return this.apollo.query<{ recentJobCardActivity: { nodes: JobCardComment[] } }>({
      query: GET_RECENT_JOB_CARD_ACTIVITY_QUERY,
      variables: { organizationId, first },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Failed to load activity');
        return data.recentJobCardActivity.nodes;
      })
    );
  }

  addJobCardComment(
    jobCardId: string,
    content: string,
    jobItemId?: string | null,
    parentCommentId?: string | null
  ): Observable<JobCardComment> {
    return this.apollo.mutate<{ addJobCardComment: JobCardComment }>({
      mutation: ADD_JOB_CARD_COMMENT_MUTATION,
      variables: { jobCardId, content, jobItemId, parentCommentId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to add comment');
        return result.data.addJobCardComment;
      })
    );
  }

  editJobCardComment(commentId: string, content: string): Observable<JobCardComment> {
    return this.apollo.mutate<{ editJobCardComment: JobCardComment }>({
      mutation: EDIT_JOB_CARD_COMMENT_MUTATION,
      variables: { commentId, content },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to edit comment');
        return result.data.editJobCardComment;
      })
    );
  }

  deleteJobCardComment(commentId: string): Observable<JobCardComment> {
    return this.apollo.mutate<{ deleteJobCardComment: JobCardComment }>({
      mutation: DELETE_JOB_CARD_COMMENT_MUTATION,
      variables: { commentId },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to delete comment');
        return result.data.deleteJobCardComment;
      })
    );
  }

  markMentionsAsRead(mentionIds: string[]): Observable<boolean> {
    return this.apollo.mutate<{ markMentionsAsRead: boolean }>({
      mutation: MARK_MENTIONS_AS_READ_MUTATION,
      variables: { mentionIds },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to mark mentions as read');
        return result.data.markMentionsAsRead;
      })
    );
  }

  // ============================================================================
  // ESTIMATE APPROVAL METHODS
  // ============================================================================

  getEstimateByShareToken(shareToken: string): Observable<{ id: string; jobCardId: string; shareToken: string; expiresAt: string; isActive: boolean; jobCard: JobCard }> {
    return this.apollo.query<{ estimateByShareToken: { id: string; jobCardId: string; shareToken: string; expiresAt: string; isActive: boolean; jobCard: JobCard } }>({
      query: GET_ESTIMATE_BY_SHARE_TOKEN_QUERY,
      variables: { shareToken },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) throw new Error('Estimate not found');
        return data.estimateByShareToken;
      })
    );
  }

  generateEstimateShareLink(jobCardId: string, expiresInHours?: number): Observable<{ id: string; shareToken: string; shareUrl: string; expiresAt: string; isActive: boolean }> {
    return this.apollo.mutate<{ generateEstimateShareLink: { id: string; shareToken: string; shareUrl: string; expiresAt: string; isActive: boolean } }>({
      mutation: GENERATE_ESTIMATE_SHARE_LINK_MUTATION,
      variables: { jobCardId, expiresInHours },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to generate share link');
        return result.data.generateEstimateShareLink;
      })
    );
  }

  revokeEstimateShareLink(shareToken: string): Observable<{ id: string; isActive: boolean }> {
    return this.apollo.mutate<{ revokeEstimateShareLink: { id: string; isActive: boolean } }>({
      mutation: REVOKE_ESTIMATE_SHARE_LINK_MUTATION,
      variables: { shareToken },
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to revoke share link');
        return result.data.revokeEstimateShareLink;
      })
    );
  }

  approveJobItems(jobCardId: string, itemIds: string[]): Observable<JobItem[]> {
    // Approve each item individually
    const approvals = itemIds.map(itemId => 
      this.approveJobItem(itemId).pipe(
        map(item => item)
      )
    );
    return new Observable(observer => {
      Promise.all(approvals.map(obs => firstValueFrom(obs))).then(
        items => {
          observer.next(items);
          observer.complete();
        },
        error => observer.error(error)
      );
    });
  }
}

