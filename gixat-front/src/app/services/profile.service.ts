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
  // Note: avatarUrl is NOT supported - use upload methods instead
}

export interface Address {
  country: string;
  city: string;
  street: string;
  phoneCountryCode: string;
}

export interface AddressInput {
  country: string;
  city: string;
  street: string;
  phoneCountryCode: string;
}

export interface Media {
  url: string;
  alt?: string | null;
}

export interface Organization {
  id: string;
  name: string;
  address: Address;
  logo: Media | null;
  createdAt: string;
}

export interface UpdateOrganizationInput {
  name?: string;
  address?: AddressInput;
}

export interface OrganizationUser {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  userType?: string | null;
  roles?: string[];
  createdAt: string;
}

export interface CreateUserInput {
  fullName: string;
  email: string;
  password: string;
  phoneNumber?: string;
  userType?: string;
  roles?: string[];
}

export interface UpdateUserInput {
  fullName?: string;
  email?: string;
  phoneNumber?: string;
  bio?: string;
  userType?: string;
  roles?: string[];
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

const GET_MY_ORGANIZATION_QUERY = gql`
  query GetMyOrganization {
    myOrganization {
      id
      name
      address {
        country
        city
        street
        phoneCountryCode
      }
      logo {
        url
        alt
      }
      createdAt
    }
  }
`;

const UPDATE_ORGANIZATION_MUTATION = gql`
  mutation UpdateOrganization($input: UpdateOrganizationInput!) {
    updateMyOrganization(input: $input) {
      id
      name
      address {
        country
        city
        street
        phoneCountryCode
      }
      logo {
        url
        alt
      }
      createdAt
    }
  }
`;

const UPLOAD_ORGANIZATION_LOGO_MUTATION = gql`
  mutation UploadOrganizationLogo($file: Upload!) {
    uploadMyOrganizationLogo(file: $file) {
      logoUrl
      message
    }
  }
`;

const DELETE_ORGANIZATION_LOGO_MUTATION = gql`
  mutation DeleteOrganizationLogo {
    deleteMyOrganizationLogo
  }
`;

const GET_ORGANIZATION_USERS_QUERY = gql`
  query GetOrganizationUsers {
    myOrganization {
      id
      users {
        id
        fullName
        email
        phoneNumber
        userType
        roles
        createdAt
      }
    }
  }
`;

const CREATE_USER_MUTATION = gql`
  mutation CreateUser($input: CreateUserInput!) {
    createUser(input: $input) {
      id
      fullName
      email
      phoneNumber
      userType
      roles
    }
  }
`;

const GET_USER_BY_ID_QUERY = gql`
  query GetUserById($id: UUID!) {
    userById(id: $id) {
      id
      fullName
      email
      phoneNumber
      bio
      userType
      roles
      createdAt
    }
  }
`;

const UPDATE_USER_MUTATION = gql`
  mutation UpdateUser($id: UUID!, $input: UpdateUserInput!) {
    updateUser(id: $id, input: $input) {
      id
      fullName
      email
      phoneNumber
      bio
      userType
      roles
    }
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
   * Generate presigned URL for direct S3 upload
   * After upload, reload profile to get the permanent avatar URL
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
   * Upload avatar via GraphQL (recommended for smaller files < 5MB)
   * Returns permanent avatar URL: https://api.gixat.com/api/media/avatars/{userId}/{fileName}
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

  /**
   * Get current user's organization
   */
  getMyOrganization(): Observable<Organization> {
    return this.apollo.query<{ myOrganization: Organization }>({
      query: GET_MY_ORGANIZATION_QUERY,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => {
        if (!result.data?.myOrganization) {
          throw new Error('Failed to load organization');
        }
        return result.data.myOrganization;
      })
    );
  }

  /**
   * Update organization information
   */
  updateOrganization(input: UpdateOrganizationInput): Observable<Organization> {
    return this.apollo.mutate<{ updateMyOrganization: Organization }>({
      mutation: UPDATE_ORGANIZATION_MUTATION,
      variables: { input }
    }).pipe(
      map(result => {
        if (!result.data?.updateMyOrganization) {
          throw new Error('Failed to update organization');
        }
        return result.data.updateMyOrganization;
      })
    );
  }

  /**
   * Upload organization logo via GraphQL
   */
  uploadOrganizationLogo(file: File): Observable<{ logoUrl: string; message?: string }> {
    return this.apollo.mutate<{ uploadMyOrganizationLogo: { logoUrl: string; message?: string } }>({
      mutation: UPLOAD_ORGANIZATION_LOGO_MUTATION,
      variables: { file },
      context: {
        useMultipart: true
      }
    }).pipe(
      map(result => {
        if (!result.data?.uploadMyOrganizationLogo) {
          throw new Error('Failed to upload logo');
        }
        return result.data.uploadMyOrganizationLogo;
      })
    );
  }

  /**
   * Delete organization logo
   */
  deleteOrganizationLogo(): Observable<boolean> {
    return this.apollo.mutate<{ deleteMyOrganizationLogo: boolean }>({
      mutation: DELETE_ORGANIZATION_LOGO_MUTATION
    }).pipe(
      map(result => {
        return result.data?.deleteMyOrganizationLogo ?? false;
      })
    );
  }

  /**
   * Get all users in the organization
   */
  getOrganizationUsers(): Observable<OrganizationUser[]> {
    return this.apollo.query<{ myOrganization: { users: OrganizationUser[] } }>({
      query: GET_ORGANIZATION_USERS_QUERY,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => {
        if (!result.data?.myOrganization?.users) {
          return [];
        }
        return result.data.myOrganization.users;
      })
    );
  }

  /**
   * Create a new user in the organization
   */
  createUser(input: CreateUserInput): Observable<OrganizationUser> {
    return this.apollo.mutate<{ createUser: OrganizationUser }>({
      mutation: CREATE_USER_MUTATION,
      variables: { input }
    }).pipe(
      map(result => {
        if (!result.data?.createUser) {
          throw new Error('Failed to create user');
        }
        return result.data.createUser;
      })
    );
  }

  /**
   * Get user by ID
   */
  getUserById(id: string): Observable<UserProfile> {
    return this.apollo.query<{ userById: UserProfile }>({
      query: GET_USER_BY_ID_QUERY,
      variables: { id },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => {
        if (!result.data?.userById) {
          throw new Error('User not found');
        }
        return result.data.userById;
      })
    );
  }

  /**
   * Update user information
   */
  updateUser(id: string, input: UpdateUserInput): Observable<UserProfile> {
    return this.apollo.mutate<{ updateUser: UserProfile }>({
      mutation: UPDATE_USER_MUTATION,
      variables: { id, input }
    }).pipe(
      map(result => {
        if (!result.data?.updateUser) {
          throw new Error('Failed to update user');
        }
        return result.data.updateUser;
      })
    );
  }
}

