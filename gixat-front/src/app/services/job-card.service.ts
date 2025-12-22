import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map } from 'rxjs';
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
  createdAt: string;
  updatedAt: string;
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
}

