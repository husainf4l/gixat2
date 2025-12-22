import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map, tap } from 'rxjs';

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  avatarUrl?: string | null;
  bio?: string | null;
  userType?: string | null;
  createdAt: string;
}

export interface UpdateProfileInput {
  fullName?: string;
  bio?: string;
  phoneNumber?: string;
  avatarUrl?: string;
}

const ME_QUERY = gql`
  query Me {
    me {
      id
      fullName
      email
      phoneNumber
      avatarUrl
      bio
      userType
      createdAt
    }
  }
`;

const UPDATE_PROFILE_MUTATION = gql`
  mutation UpdateProfile($input: UpdateProfileInput!) {
    updateMyProfile(input: $input) {
      id
      fullName
      bio
      phoneNumber
      avatarUrl
    }
  }
`;

const GENERATE_AVATAR_UPLOAD_URL_MUTATION = gql`
  mutation GenerateAvatarUploadUrl($fileName: String!, $contentType: String!) {
    generateAvatarUploadUrl(fileName: $fileName, contentType: $contentType)
  }
`;

const UPLOAD_AVATAR_MUTATION = gql`
  mutation UploadAvatar($file: Upload!) {
    uploadMyAvatar(file: $file) {
      avatarUrl
      message
    }
  }
`;

const DELETE_AVATAR_MUTATION = gql`
  mutation DeleteAvatar {
    deleteMyAvatar
  }
`;

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private apollo = inject(Apollo);

  /**
   * Get current user profile
   */
  getMyProfile(): Observable<UserProfile> {
    return this.apollo.query<{ me: UserProfile }>({
      query: ME_QUERY,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => {
        if (!result.data?.me) {
          throw new Error('Failed to load profile');
        }
        return result.data.me;
      })
    );
  }

  /**
   * Update user profile
   */
  updateProfile(input: UpdateProfileInput): Observable<UserProfile> {
    return this.apollo.mutate<{ updateMyProfile: UserProfile }>({
      mutation: UPDATE_PROFILE_MUTATION,
      variables: { input }
    }).pipe(
      map(result => {
        if (!result.data?.updateMyProfile) {
          throw new Error('Failed to update profile');
        }
        return result.data.updateMyProfile;
      })
    );
  }

  /**
   * Generate presigned URL for direct S3 upload (recommended for production)
   */
  generateAvatarUploadUrl(fileName: string, contentType: string): Observable<string> {
    return this.apollo.mutate<{ generateAvatarUploadUrl: string }>({
      mutation: GENERATE_AVATAR_UPLOAD_URL_MUTATION,
      variables: { fileName, contentType }
    }).pipe(
      map(result => {
        if (!result.data?.generateAvatarUploadUrl) {
          throw new Error('Failed to generate upload URL');
        }
        return result.data.generateAvatarUploadUrl;
      })
    );
  }

  /**
   * Upload avatar directly to S3 using presigned URL
   */
  uploadToS3(uploadUrl: string, file: File): Observable<void> {
    return new Observable(observer => {
      fetch(uploadUrl, {
        method: 'PUT',
        headers: {
          'Content-Type': file.type
        },
        body: file
      })
        .then(response => {
          if (!response.ok) {
            throw new Error(`S3 upload failed: ${response.status} ${response.statusText}`);
          }
          observer.next();
          observer.complete();
        })
        .catch(error => {
          observer.error(error);
        });
    });
  }

  /**
   * Extract base URL from presigned S3 URL (removes query parameters)
   * Example: https://bucket.s3.region.amazonaws.com/path/to/file.jpg?X-Amz-Expires=... 
   *       -> https://bucket.s3.region.amazonaws.com/path/to/file.jpg
   */
  extractBaseUrlFromPresignedUrl(presignedUrl: string): string {
    try {
      const url = new URL(presignedUrl);
      // Return the base URL without query parameters
      return `${url.protocol}//${url.host}${url.pathname}`;
    } catch (error) {
      throw new Error('Invalid presigned URL format');
    }
  }

  /**
   * Upload avatar via GraphQL (alternative method for smaller files)
   */
  uploadAvatar(file: File): Observable<{ avatarUrl: string; message?: string }> {
    return this.apollo.mutate<{ uploadMyAvatar: { avatarUrl: string; message?: string } }>({
      mutation: UPLOAD_AVATAR_MUTATION,
      variables: { file },
      context: {
        useMultipart: true
      }
    }).pipe(
      map(result => {
        if (!result.data?.uploadMyAvatar) {
          throw new Error('Failed to upload avatar');
        }
        return result.data.uploadMyAvatar;
      })
    );
  }

  /**
   * Delete user avatar
   */
  deleteAvatar(): Observable<boolean> {
    return this.apollo.mutate<{ deleteMyAvatar: boolean }>({
      mutation: DELETE_AVATAR_MUTATION
    }).pipe(
      map(result => {
        return result.data?.deleteMyAvatar ?? false;
      })
    );
  }
}

