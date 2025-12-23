import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map } from 'rxjs';

export interface VehicleWithCustomer {
  id: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  vin?: string | null;
  color?: string | null;
  customer: {
    id: string;
    firstName: string;
    lastName: string;
    phoneNumber?: string | null;
  };
}

const ALL_CARS_QUERY = gql`
  query AllCars {
    cars {
      edges {
        node {
          id
          make
          model
          year
          licensePlate
          vin
          color
          customer {
            id
            firstName
            lastName
            phoneNumber
          }
        }
      }
    }
  }
`;

const GET_CAR_BY_ID_QUERY = gql`
  query GetCarById($id: UUID!) {
    carById(id: $id) {
      id
      make
      model
      year
      licensePlate
      vin
      color
      customer {
        id
        firstName
        lastName
        email
        phoneNumber
      }
      sessions {
        id
        status
        createdAt
      }
    }
  }
`;

@Injectable({
  providedIn: 'root'
})
export class VehicleService {
  private apollo = inject(Apollo);

  getAllVehicles(): Observable<VehicleWithCustomer[]> {
    return this.apollo.query<any>({
      query: ALL_CARS_QUERY,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data.cars.edges.map((edge: any) => edge.node))
    );
  }

  getVehicleById(id: string): Observable<any> {
    return this.apollo.query<any>({
      query: GET_CAR_BY_ID_QUERY,
      variables: { id },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data.carById)
    );
  }
}
