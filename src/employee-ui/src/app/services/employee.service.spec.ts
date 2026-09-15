import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../environments/environment';
import { Employee } from '../models/employee';
import { EmployeeService } from './employee.service';

describe('EmployeeService', () => {
  let service: EmployeeService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(EmployeeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should GET employees from the configured API base URL', () => {
    const mockEmployees: Employee[] = [
      { id: 1, name: 'Alice', department: 'Engineering', salary: 95000 }
    ];

    service.getEmployees().subscribe((employees) => {
      expect(employees).toEqual(mockEmployees);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees`);
    expect(req.request.method).toBe('GET');
    req.flush(mockEmployees);
  });

  it('should propagate errors to the caller', () => {
    let receivedError: unknown;

    service.getEmployees().subscribe({
      next: () => fail('expected an error, not a value'),
      error: (err) => (receivedError = err)
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees`);
    req.flush('server error', { status: 500, statusText: 'Internal Server Error' });

    expect(receivedError).toBeTruthy();
  });
});
