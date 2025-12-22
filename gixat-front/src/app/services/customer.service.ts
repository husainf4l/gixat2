import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map, shareReplay } from 'rxjs';

export interface Address {
  id?: string;
  country: string;
  city: string;
  street: string;
  phoneCountryCode: string;
}

export interface Car {
  id: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  vin?: string | null;
  color?: string | null;
}

export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phoneNumber: string;
  address?: Address | null;
  cars: Car[];
  // Activity metrics
  lastSessionDate?: string | null;
  totalVisits?: number;
  totalSpent?: number;
  activeJobCards?: number;
  totalCars?: number;
}

export interface CustomerStatistics {
  totalCustomers: number;
  customersThisMonth: number;
  activeCustomers: number;
  totalRevenue: number;
}

export interface CustomerDetail {
  customer: (Omit<Customer, 'cars'> & { cars: Car[] }) | null;
  jobCards: JobCardSummary[];
  sessions: SessionSummary[];
}

export interface JobCardSummary {
  id: string;
  status: string;
  totalEstimatedCost: string;
  totalActualCost: string;
  createdAt: string;
  car: Pick<Car, 'make' | 'model' | 'licensePlate'> | null;
}

export interface SessionSummary {
  id: string;
  status: string;
  createdAt: string;
  carId: string;
  car: Pick<Car, 'make' | 'model' | 'licensePlate'> | null;
}

export interface CreateCustomerInput {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email?: string | null;
  country?: string | null;
  city?: string | null;
  street?: string | null;
}

export interface CreateCarInput {
  customerId: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  vin?: string | null;
  color?: string | null;
}

const SEARCH_CUSTOMERS_QUERY = gql`
  query SearchCustomers($query: String!, $first: Int) {
    searchCustomers(query: $query, first: $first) {
      pageInfo {
        hasNextPage
        endCursor
      }
      totalCount
      edges {
        node {
          id
          firstName
          lastName
          phoneNumber
          cars {
            make
            model
            licensePlate
          }
        }
      }
    }
  }
`;

const CUSTOMERS_QUERY = gql`
  query Customers($first: Int, $order: [CustomerSortInput!], $after: String) {
    customers(first: $first, order: $order, after: $after) {
      pageInfo {
        hasNextPage
        hasPreviousPage
        startCursor
        endCursor
      }
      totalCount
      edges {
        cursor
        node {
          id
          firstName
          lastName
          email
          phoneNumber
          address {
            city
          }
          cars {
            id
          }
          lastSessionDate
          totalVisits
          totalSpent
          activeJobCards
          totalCars
        }
      }
    }
  }
`;

const CUSTOMER_STATISTICS_QUERY = gql`
  query CustomerStatistics {
    customerStatistics {
      totalCustomers
      customersThisMonth
      activeCustomers
      totalRevenue
    }
  }
`;

const EXPORT_CUSTOMERS_CSV_MUTATION = gql`
  mutation ExportCustomers {
    exportCustomersToCsv
  }
`;

const CUSTOMER_DETAIL_QUERY = gql`
  query CustomerDetail($id: UUID!) {
    customerById(id: $id) {
      id
      firstName
      lastName
      email
      phoneNumber
      address {
        country
        city
        street
      }
      cars {
        id
        make
        model
        year
        licensePlate
        color
        vin
      }
    }
    jobCards(where: { customerId: { eq: $id } }, order: [{ createdAt: DESC }]) {
      id
      status
      totalEstimatedCost
      totalActualCost
      createdAt
      car {
        make
        model
        licensePlate
      }
    }
    sessions(where: { customerId: { eq: $id } }, order: [{ createdAt: DESC }]) {
      edges {
        node {
          id
          status
          createdAt
          carId
          car {
            make
            model
            licensePlate
          }
        }
      }
    }
  }
`;

const CREATE_CUSTOMER_MUTATION = gql`
  mutation CreateCustomer($input: CreateCustomerInput!) {
    createCustomer(input: $input) {
      id
      firstName
      lastName
      email
      phoneNumber
    }
  }
`;

const CREATE_CAR_MUTATION = gql`
  mutation CreateCar($input: CreateCarInput!) {
    createCar(input: $input) {
      id
      make
      model
      year
      licensePlate
      color
      vin
    }
  }
`;

const CREATE_SESSION_MUTATION = gql`
  mutation CreateSession($carId: UUID!, $customerId: UUID!) {
    createSession(carId: $carId, customerId: $customerId) {
      id
      status
      createdAt
    }
  }
`;

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private apollo = inject(Apollo);

  searchCustomers(query: string, first: number = 50): Observable<Customer[]> {
    return this.apollo.query<{ searchCustomers: { edges: { node: Customer }[] } }>({
      query: SEARCH_CUSTOMERS_QUERY,
      variables: { query, first },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data?.searchCustomers?.edges) {
          return [];
        }
        return data.searchCustomers.edges.map(edge => edge.node);
      }),
    );
  }

  getCustomers(first: number = 100, order?: unknown, after?: string): Observable<Customer[]> {
    return this.apollo.query<{ customers: { edges: { node: Customer }[] } }>({
      query: CUSTOMERS_QUERY,
      variables: { first, order, after },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data?.customers?.edges) {
          throw new Error('Failed to load customers');
        }
        return data.customers.edges.map(edge => edge.node);
      }),
    );
  }

  getCustomerDetail(id: string): Observable<CustomerDetail> {
    return this.apollo.query<{ 
      customerById: CustomerDetail['customer']; 
      jobCards: JobCardSummary[]; 
      sessions: { edges: { node: SessionSummary }[] } 
    }>({
      query: CUSTOMER_DETAIL_QUERY,
      variables: { id },
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data) {
          throw new Error('Failed to load customer detail');
        }
        return {
          customer: data.customerById,
          jobCards: data.jobCards,
          sessions: data.sessions.edges.map(edge => edge.node),
        };
      }),
      shareReplay(1),
    );
  }

  createCustomer(input: CreateCustomerInput): Observable<Customer> {
    return this.apollo.mutate<{ createCustomer: Customer }>({
      mutation: CREATE_CUSTOMER_MUTATION,
      variables: { input },
    }).pipe(
      map(result => {
        if (!result.data?.createCustomer) {
          throw new Error('Failed to create customer');
        }
        return result.data.createCustomer;
      }),
    );
  }

  createCar(input: CreateCarInput): Observable<Car> {
    return this.apollo.mutate<{ createCar: Car }>({
      mutation: CREATE_CAR_MUTATION,
      variables: { input },
    }).pipe(
      map(result => {
        if (!result.data?.createCar) {
          throw new Error('Failed to create car');
        }
        return result.data.createCar;
      }),
    );
  }

  getCustomerStatistics(): Observable<CustomerStatistics> {
    return this.apollo.query<{ customerStatistics: CustomerStatistics }>({
      query: CUSTOMER_STATISTICS_QUERY,
      fetchPolicy: 'network-only',
    }).pipe(
      map(({ data }) => {
        if (!data?.customerStatistics) {
          throw new Error('Failed to load customer statistics');
        }
        return data.customerStatistics;
      }),
    );
  }

  exportCustomersToCsv(): Observable<string> {
    return this.apollo.mutate<{ exportCustomersToCsv: string }>({
      mutation: EXPORT_CUSTOMERS_CSV_MUTATION,
    }).pipe(
      map(result => {
        if (!result.data?.exportCustomersToCsv) {
          throw new Error('Failed to export customers');
        }
        return result.data.exportCustomersToCsv;
      }),
    );
  }

  createSession(carId: string, customerId: string): Observable<{ id: string; status: string; createdAt: string }> {
    return this.apollo.mutate<{ createSession: { id: string; status: string; createdAt: string } }>({
      mutation: CREATE_SESSION_MUTATION,
      variables: { carId, customerId },
    }).pipe(
      map(result => {
        if (!result.data?.createSession) {
          throw new Error('Failed to create session');
        }
        return result.data.createSession;
      }),
    );
  }
}
