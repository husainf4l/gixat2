import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrganizationSetup } from './organization-setup';

describe('OrganizationSetup', () => {
  let component: OrganizationSetup;
  let fixture: ComponentFixture<OrganizationSetup>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrganizationSetup]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OrganizationSetup);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
