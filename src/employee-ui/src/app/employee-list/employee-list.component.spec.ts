import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Employee } from '../models/employee';
import { EmployeeService } from '../services/employee.service';
import { EmployeeListComponent } from './employee-list.component';

describe('EmployeeListComponent', () => {
  const mockEmployees: Employee[] = [
    { id: 1, name: 'Alice', department: 'Engineering', salary: 95000 },
    { id: 2, name: 'Bob', department: 'Finance', salary: 82000 }
  ];

  function setup(employeeServiceStub: Partial<EmployeeService>) {
    TestBed.configureTestingModule({
      imports: [EmployeeListComponent],
      providers: [{ provide: EmployeeService, useValue: employeeServiceStub }]
    });

    const fixture = TestBed.createComponent(EmployeeListComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('should show a loading state before the request resolves', () => {
    const { component } = setup({ getEmployees: () => of(mockEmployees) });
    expect(component.loading).toBeTrue();
  });

  it('should populate employees and clear loading on success', () => {
    const { fixture, component } = setup({ getEmployees: () => of(mockEmployees) });

    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBeNull();
    expect(component.employees).toEqual(mockEmployees);
  });

  it('should render a table row per employee with formatted salary', () => {
    const { fixture } = setup({ getEmployees: () => of(mockEmployees) });

    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Alice');
    expect(rows[0].textContent).toContain('$95,000.00');
  });

  it('should show a friendly error message when the request fails', () => {
    const { fixture, component } = setup({
      getEmployees: () => throwError(() => new Error('network down'))
    });

    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toContain('Unable to load employee data');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeTruthy();
  });
});
