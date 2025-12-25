import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApolloClient, InMemoryCache, ApolloLink } from '@apollo/client/core';
import { Apollo } from 'apollo-angular';
import { HttpClientTestingModule } from '@angular/common/http/testing';

import { HealthCheckComponent } from './health-check';

describe('HealthCheckComponent', () => {
  let component: HealthCheckComponent;
  let fixture: ComponentFixture<HealthCheckComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HealthCheckComponent, HttpClientTestingModule],
      providers: [
        Apollo,
        {
          provide: ApolloClient,
          useFactory: () => {
            return new ApolloClient({
              cache: new InMemoryCache(),
              link: ApolloLink.empty()
            });
          }
        }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HealthCheckComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
