import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map } from 'rxjs';

export interface SessionCustomer {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phoneNumber?: string | null;
}

export interface SessionCar {
  id: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  color?: string | null;
  vin?: string | null;
  mileage?: number | null;
}

export interface SessionLog {
  id: string;
  fromStatus: string;
  toStatus: string;
  notes?: string;
  createdAt: string;
}

export interface AppMedia {
  id: string;
  url: string;
  alt?: string | null;
  type?: string | null;
  createdAt?: string;
}

export interface SessionMedia {
  id: string;
  stage: string;
  media: AppMedia;
  createdAt?: string;
  isPending?: boolean;
  status?: 'pending' | 'uploading' | 'uploaded' | 'error';
}

export interface Session {
  id: string;
  status: string;
  createdAt: string;
  carId: string;
  customerId: string;
  customer: SessionCustomer | null;
  car: SessionCar | null;
  // Workflow steps
  intakeNotes?: string | null;
  intakeRequests?: string | null;
  customerRequests?: string | null;
  inspectionNotes?: string | null;
  inspectionRequests?: string | null;
  testDriveNotes?: string | null;
  testDriveRequests?: string | null;
  initialReport?: string | null;
  logs: SessionLog[];
  media?: SessionMedia[];
}

export interface SessionsConnection {
  edges: {
    node: Session;
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

const GET_SESSIONS_QUERY = gql`
  query GetSessions($first: Int, $after: String, $where: GarageSessionFilterInput, $order: [GarageSessionSortInput!]) {
    sessions(first: $first, after: $after, where: $where, order: $order) {
      edges {
        node {
          id
          status
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
        hasPreviousPage
        startCursor
        endCursor
      }
      totalCount
    }
  }
`;

const GET_SESSION_BY_ID_QUERY = gql`
  query GetSessionById($id: UUID!) {
    sessionById(id: $id) {
      id
      status
      createdAt
      intakeNotes
      intakeRequests
      customerRequests
      inspectionNotes
      inspectionRequests
      testDriveNotes
      testDriveRequests
      initialReport
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
      logs {
        id
        fromStatus
        toStatus
        notes
        createdAt
      }
      media {
        id
        stage
        media {
          id
          url
          alt
          type
          createdAt
        }
        createdAt
      }
    }
  }
`;

const UPDATE_INTAKE_MUTATION = gql`
  mutation UpdateIntake($sessionId: UUID!, $intakeNotes: String, $intakeRequests: String) {
    updateIntake(sessionId: $sessionId, intakeNotes: $intakeNotes, intakeRequests: $intakeRequests) {
      id
      status
      intakeNotes
      intakeRequests
    }
  }
`;

const UPDATE_CUSTOMER_REQUESTS_MUTATION = gql`
  mutation UpdateCustomerRequests($sessionId: UUID!, $customerRequests: String) {
    updateCustomerRequests(sessionId: $sessionId, customerRequests: $customerRequests) {
      id
      status
      customerRequests
    }
  }
`;

const UPDATE_INSPECTION_MUTATION = gql`
  mutation UpdateInspection($sessionId: UUID!, $inspectionNotes: String, $inspectionRequests: String) {
    updateInspection(sessionId: $sessionId, inspectionNotes: $inspectionNotes, inspectionRequests: $inspectionRequests) {
      id
      status
      inspectionNotes
      inspectionRequests
    }
  }
`;

const UPDATE_TEST_DRIVE_MUTATION = gql`
  mutation UpdateTestDrive($sessionId: UUID!, $testDriveNotes: String, $testDriveRequests: String) {
    updateTestDrive(sessionId: $sessionId, testDriveNotes: $testDriveNotes, testDriveRequests: $testDriveRequests) {
      id
      status
      testDriveNotes
      testDriveRequests
    }
  }
`;

const GENERATE_INITIAL_REPORT_MUTATION = gql`
  mutation GenerateInitialReport($sessionId: UUID!, $report: String!) {
    generateInitialReport(sessionId: $sessionId, report: $report) {
      id
      status
      initialReport
    }
  }
`;

const CREATE_JOB_CARD_MUTATION = gql`
  mutation CreateJobCardFromSession($sessionId: UUID!) {
    createJobCardFromSession(sessionId: $sessionId) {
      id
      status
    }
  }
`;

const UPLOAD_MEDIA_TO_SESSION_MUTATION = gql`
  mutation UploadMediaToSession($sessionId: UUID!, $file: Upload!, $stage: SessionStage!, $alt: String) {
    uploadMediaToSession(sessionId: $sessionId, file: $file, stage: $stage, alt: $alt) {
      id
      stage
      media {
        id
        url
        alt
      }
    }
  }
`;

const GET_PRESIGNED_URL_MUTATION = gql`
  mutation PresignedUploadUrl($sessionId: UUID!, $stage: SessionStage!, $files: [SessionFileUploadRequestInput!]!) {
    presignedUploadUrl(sessionId: $sessionId, stage: $stage, files: $files) {
      uploadUrl
      fileKey
    }
  }
`;

const PROCESS_UPLOADED_FILE_MUTATION = gql`
  mutation ProcessUploadedFile($fileKey: String!, $alt: String) {
    processUploadedFile(fileKey: $fileKey, alt: $alt) {
      id
      url
      alt
    }
  }
`;

const PROCESS_BULK_SESSION_UPLOADS_MUTATION = gql`
  mutation ProcessBulkSessionUploads($sessionId: UUID!, $files: [BulkSessionFileUploadRequestInput!]!) {
    processBulkSessionUploads(sessionId: $sessionId, files: $files) {
      fileKey
      success
      sessionMedia {
        id
        stage
        media {
          id
          url
          alt
        }
      }
      errorMessage
    }
  }
`;

const DELETE_SESSION_MEDIA_MUTATION = gql`
  mutation DeleteSessionMedia($mediaId: UUID!) {
    deleteSessionMedia(mediaId: $mediaId)
  }
`;

@Injectable({ providedIn: 'root' })
export class SessionService {
  private apollo = inject(Apollo);

  getSessions(
    first: number = 20,
    after?: string | null,
    where?: any,
    order?: any[]
  ): Observable<SessionsConnection> {
    return this.apollo.query<{ sessions: SessionsConnection }>({
      query: GET_SESSIONS_QUERY,
      variables: { 
        first, 
        after,
        where,
        order: order || [{ createdAt: 'DESC' }]
      },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data?.sessions) {
          throw new Error('Failed to load sessions');
        }
        return data.sessions;
      }),
    );
  }

  getSessionById(id: string): Observable<Session> {
    return this.apollo.query<{ sessionById: Session }>({
      query: GET_SESSION_BY_ID_QUERY,
      variables: { id },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data?.sessionById) {
          throw new Error('Session not found');
        }
        return data.sessionById;
      }),
    );
  }

  updateSessionStep(sessionId: string, stepId: string, notes: string, requests?: string): Observable<Session> {
    let mutation;
    let variables: any = { sessionId };
    
    // Map step id to the correct mutation and variables
    switch (stepId) {
      case 'intake':
        mutation = UPDATE_INTAKE_MUTATION;
        variables.intakeNotes = notes;
        variables.intakeRequests = requests;
        break;
      case 'customerRequests':
        mutation = UPDATE_CUSTOMER_REQUESTS_MUTATION;
        // For customer requests, we use the requests parameter if provided, otherwise notes
        variables.customerRequests = requests || notes;
        break;
      case 'inspection':
        mutation = UPDATE_INSPECTION_MUTATION;
        variables.inspectionNotes = notes;
        variables.inspectionRequests = requests;
        break;
      case 'testDrive':
        mutation = UPDATE_TEST_DRIVE_MUTATION;
        variables.testDriveNotes = notes;
        variables.testDriveRequests = requests;
        break;
      case 'initialReport':
        mutation = GENERATE_INITIAL_REPORT_MUTATION;
        variables.report = notes;
        break;
      default:
        throw new Error(`Unknown step: ${stepId}`);
    }

    return this.apollo.mutate<any>({
      mutation,
      variables,
    }).pipe(
      map(result => {
        if (!result.data) {
          throw new Error('Failed to update session step');
        }
        // Return the data from whichever mutation was called
        return result.data[Object.keys(result.data)[0]];
      }),
    );
  }

  generateJobCard(sessionId: string): Observable<any> {
    return this.apollo.mutate({
      mutation: CREATE_JOB_CARD_MUTATION,
      variables: { sessionId },
    }).pipe(
      map(result => {
        if (!result.data) {
          throw new Error('Failed to generate job card');
        }
        return result.data;
      }),
    );
  }

  uploadMediaToSession(sessionId: string, file: File, stage?: string, alt?: string): Observable<any> {
    return this.apollo.mutate<{ uploadMediaToSession: { id: string; url: string; alt?: string } }>({
      mutation: UPLOAD_MEDIA_TO_SESSION_MUTATION,
      variables: { sessionId, file, stage, alt },
      context: {
        useMultipart: true
      }
    }).pipe(
      map(result => {
        if (!result.data?.uploadMediaToSession) {
          throw new Error('Failed to upload media');
        }
        return result.data.uploadMediaToSession;
      }),
    );
  }

  // Presigned URL flow - matches backend implementation
  getPresignedUrls(
    sessionId: string, 
    stage: string, 
    files: { fileName: string; contentType: string }[]
  ): Observable<{ uploadUrl: string; fileKey: string }[]> {
    return this.apollo.mutate<{ presignedUploadUrl: { uploadUrl: string; fileKey: string }[] }>({
      mutation: GET_PRESIGNED_URL_MUTATION,
      variables: { sessionId, stage, files }
    }).pipe(
      map(result => {
        if (!result.data?.presignedUploadUrl) {
          throw new Error('Failed to get presigned URLs');
        }
        return result.data.presignedUploadUrl;
      }),
    );
  }

  async uploadToS3(uploadUrl: string, file: File): Promise<void> {
    const response = await fetch(uploadUrl, {
      method: 'PUT',
      body: file,
      headers: {
        'Content-Type': file.type,
      }
    });

    if (!response.ok) {
      throw new Error(`S3 upload failed: ${response.statusText}`);
    }
  }

  processUploadedFile(fileKey: string, alt?: string): Observable<{ id: string; url: string; alt?: string }> {
    return this.apollo.mutate<{ processUploadedFile: { id: string; url: string; alt?: string } }>({
      mutation: PROCESS_UPLOADED_FILE_MUTATION,
      variables: { fileKey, alt }
    }).pipe(
      map(result => {
        if (!result.data?.processUploadedFile) {
          throw new Error('Failed to process uploaded file');
        }
        return result.data.processUploadedFile;
      }),
    );
  }

  processBulkSessionUploads(
    sessionId: string,
    files: { fileKey: string; stage: string; alt?: string }[]
  ): Observable<{ fileKey: string; success: boolean; sessionMedia?: SessionMedia; errorMessage?: string }[]> {
    return this.apollo.mutate<{ processBulkSessionUploads: { fileKey: string; success: boolean; sessionMedia?: SessionMedia; errorMessage?: string }[] }>({
      mutation: PROCESS_BULK_SESSION_UPLOADS_MUTATION,
      variables: { sessionId, files }
    }).pipe(
      map(result => {
        if (!result.data?.processBulkSessionUploads) {
          throw new Error('Failed to process bulk session uploads');
        }
        return result.data.processBulkSessionUploads;
      }),
    );
  }

  deleteSessionMedia(mediaId: string): Observable<boolean> {
    return this.apollo.mutate<{ deleteSessionMedia: boolean }>({
      mutation: DELETE_SESSION_MEDIA_MUTATION,
      variables: { mediaId }
    }).pipe(
      map(result => {
        if (result.data?.deleteSessionMedia === undefined) {
          throw new Error('Failed to delete media');
        }
        return result.data.deleteSessionMedia;
      }),
    );
  }
}
