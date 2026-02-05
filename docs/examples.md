# Examples

This document provides complete examples of source files and their generated tests.

## Component Examples

### Standalone Component

**Source: `user-profile.component.ts`**
```typescript
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserService } from '../services/user.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="profile">
      <h1>{{ user?.name }}</h1>
    </div>
  `
})
export class UserProfileComponent {
  private userService = inject(UserService);
  user$ = this.userService.getCurrentUser();
}
```

**Generated: `user-profile.component.spec.ts`**
```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { UserProfileComponent } from './user-profile.component';

describe('UserProfileComponent', () => {
  let component: UserProfileComponent;
  let fixture: ComponentFixture<UserProfileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserProfileComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render component', () => {
    expect(fixture.nativeElement).toBeTruthy();
  });
});
```

### Component with Router

**Source: `navigation.component.ts`**
```typescript
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-navigation',
  standalone: true,
  template: `<nav>...</nav>`
})
export class NavigationComponent {
  private router = inject(Router);

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
```

**Generated: `navigation.component.spec.ts`**
```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { NavigationComponent } from './navigation.component';

describe('NavigationComponent', () => {
  let component: NavigationComponent;
  let fixture: ComponentFixture<NavigationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavigationComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NavigationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render component', () => {
    expect(fixture.nativeElement).toBeTruthy();
  });
});
```

## Service Examples

### HTTP Service with Multiple Methods

**Source: `user.service.ts`**
```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private baseUrl = inject(API_BASE_URL);

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.baseUrl}/users`);
  }

  getUserById(id: string): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/users/${id}`);
  }

  createUser(user: CreateUserRequest): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}/users`, user);
  }

  updateUser(id: string, user: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/users/${id}`, user);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/users/${id}`);
  }
}
```

**Generated: `user.service.spec.ts`**
```typescript
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL } from '../config/api.config';
import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://localhost:3000' },
      ],
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getUsers', () => {
    it('should make GET request', () => {
      const mockResponse = {};

      service.getUsers().subscribe((response) => {
        expect(response).toBeDefined();
      });

      const req = httpMock.expectOne((request) => request.method === 'GET');
      req.flush(mockResponse);
    });
  });

  describe('getUserById', () => {
    it('should make GET request', () => {
      const mockResponse = {};

      service.getUserById('test-value').subscribe((response) => {
        expect(response).toBeDefined();
      });

      const req = httpMock.expectOne((request) => request.method === 'GET');
      req.flush(mockResponse);
    });
  });

  describe('createUser', () => {
    it('should make POST request', () => {
      const mockResponse = {};

      service.createUser({} as any).subscribe((response) => {
        expect(response).toBeDefined();
      });

      const req = httpMock.expectOne((request) => request.method === 'POST');
      req.flush(mockResponse);
    });
  });

  describe('updateUser', () => {
    it('should make PUT request', () => {
      const mockResponse = {};

      service.updateUser('test-value', {} as any).subscribe((response) => {
        expect(response).toBeDefined();
      });

      const req = httpMock.expectOne((request) => request.method === 'PUT');
      req.flush(mockResponse);
    });
  });

  describe('deleteUser', () => {
    it('should make DELETE request', () => {
      const mockResponse = {};

      service.deleteUser('test-value').subscribe((response) => {
        expect(response).toBeDefined();
      });

      const req = httpMock.expectOne((request) => request.method === 'DELETE');
      req.flush(mockResponse);
    });
  });
});
```

## Guard Examples

### Functional Auth Guard

**Source: `auth.guard.ts`**
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.hasValidToken()) {
    return true;
  }

  authService.setRedirectUrl(state.url);
  router.navigate(['/login']);
  return false;
};
```

**Generated: `auth.guard.spec.ts`**
```typescript
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { vi, Mock } from 'vitest';
import { AuthService } from '../../shared/services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let mockAuthService: {
    hasValidToken: Mock;
    setRedirectUrl: Mock;
  };
  let router: Router;

  beforeEach(() => {
    mockAuthService = {
      hasValidToken: vi.fn(),
      setRedirectUrl: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
      ],
    });

    router = TestBed.inject(Router);
  });

  it('should allow activation when authenticated', () => {
    mockAuthService.hasValidToken.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() => {
      const mockRoute = {} as ActivatedRouteSnapshot;
      const mockState = { url: '/test' } as RouterStateSnapshot;
      return authGuard(mockRoute, mockState);
    });

    expect(result).toBe(true);
  });

  it('should deny activation when not authenticated', () => {
    mockAuthService.hasValidToken.mockReturnValue(false);
    const navigateSpy = vi.spyOn(router, 'navigate').mockImplementation(() => Promise.resolve(true));

    const result = TestBed.runInInjectionContext(() => {
      const mockRoute = {} as ActivatedRouteSnapshot;
      const mockState = { url: '/protected' } as RouterStateSnapshot;
      return authGuard(mockRoute, mockState);
    });

    expect(result).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });
});
```

## Interceptor Examples

### Functional Auth Interceptor

**Source: `auth.interceptor.ts`**
```typescript
import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Skip auth endpoints
  if (req.url.includes('/auth/login') || req.url.includes('/auth/register')) {
    return next(req);
  }

  const token = authService.getAccessToken();
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req);
};
```

**Generated: `auth.interceptor.spec.ts`**
```typescript
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { vi, Mock } from 'vitest';
import { AuthService } from '../../shared/services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let mockAuthService: {
    getAccessToken: Mock;
    getRefreshToken: Mock;
    refreshToken: Mock;
    logout: Mock;
    setRedirectUrl: Mock;
  };

  beforeEach(() => {
    mockAuthService = {
      getAccessToken: vi.fn(),
      getRefreshToken: vi.fn(),
      refreshToken: vi.fn(),
      logout: vi.fn(),
      setRedirectUrl: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should add auth header to requests', () => {
    mockAuthService.getAccessToken.mockReturnValue('test-token');

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(true);
    req.flush({});
  });

  it('should skip auth header for login endpoint', () => {
    mockAuthService.getAccessToken.mockReturnValue('test-token');

    httpClient.post('/api/auth/login', {}).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
```

## Pipe Examples

### Date Format Pipe

**Source: `date-format.pipe.ts`**
```typescript
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateFormat',
  standalone: true
})
export class DateFormatPipe implements PipeTransform {
  transform(value: Date | string, format: string = 'short'): string {
    const date = new Date(value);
    // ... formatting logic
    return date.toLocaleDateString();
  }
}
```

**Generated: `date-format.pipe.spec.ts`**
```typescript
import { DateFormatPipe } from './date-format.pipe';

describe('DateFormatPipe', () => {
  let pipe: DateFormatPipe;

  beforeEach(() => {
    pipe = new DateFormatPipe();
  });

  it('should create pipe', () => {
    expect(pipe).toBeTruthy();
  });

  it('should transform value', () => {
    const result = pipe.transform('test');
    expect(result).toBeDefined();
  });
});
```

## Directive Examples

### Highlight Directive

**Source: `highlight.directive.ts`**
```typescript
import { Directive, ElementRef, HostListener, inject } from '@angular/core';

@Directive({
  selector: '[appHighlight]',
  standalone: true
})
export class HighlightDirective {
  private el = inject(ElementRef);

  @HostListener('mouseenter')
  onMouseEnter(): void {
    this.highlight('yellow');
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    this.highlight('');
  }

  private highlight(color: string): void {
    this.el.nativeElement.style.backgroundColor = color;
  }
}
```

**Generated: `highlight.directive.spec.ts`**
```typescript
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Component } from '@angular/core';
import { HighlightDirective } from './highlight.directive';

@Component({
  template: '<input type="text" />',
  standalone: true,
  imports: [HighlightDirective],
})
class TestHostComponent {}

describe('HighlightDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('should create host component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should apply directive', () => {
    const input = fixture.nativeElement.querySelector('input');
    expect(input).toBeTruthy();
  });
});
```

## Resolver Examples

### Functional Data Resolver

**Source: `user.resolver.ts`**
```typescript
import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { UserService } from '../services/user.service';
import { User } from '../models/user.model';

export const userResolver: ResolveFn<User> = (route, state) => {
  const userService = inject(UserService);
  const userId = route.paramMap.get('id');
  return userService.getUserById(userId!);
};
```

**Generated: `user.resolver.spec.ts`**
```typescript
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { userResolver } from './user.resolver';

describe('userResolver', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should resolve data', () => {
    const result = TestBed.runInInjectionContext(() => {
      const mockRoute = {} as ActivatedRouteSnapshot;
      const mockState = { url: '/test' } as RouterStateSnapshot;
      return userResolver(mockRoute, mockState);
    });

    expect(result).toBeDefined();
  });
});
```

## Enhancing Generated Tests

The generated tests provide a foundation. Here are examples of how to enhance them:

### Adding Meaningful Test Data

```typescript
// Before (generated)
it('should make GET request', () => {
  const mockResponse = {};
  service.getUsers().subscribe((response) => {
    expect(response).toBeDefined();
  });
  // ...
});

// After (enhanced)
it('should return list of users', () => {
  const mockUsers: User[] = [
    { id: '1', name: 'John Doe', email: 'john@example.com' },
    { id: '2', name: 'Jane Smith', email: 'jane@example.com' },
  ];

  service.getUsers().subscribe((users) => {
    expect(users).toHaveLength(2);
    expect(users[0].name).toBe('John Doe');
  });

  const req = httpMock.expectOne(`${baseUrl}/users`);
  expect(req.request.method).toBe('GET');
  req.flush(mockUsers);
});
```

### Testing Error Cases

```typescript
// Add error handling tests
it('should handle 404 error', () => {
  service.getUserById('nonexistent').subscribe({
    error: (error) => {
      expect(error.status).toBe(404);
    }
  });

  const req = httpMock.expectOne((request) => request.method === 'GET');
  req.flush('Not Found', { status: 404, statusText: 'Not Found' });
});
```

### Testing Component Interactions

```typescript
// Add interaction tests
it('should navigate to user profile on click', () => {
  const navigateSpy = vi.spyOn(router, 'navigate');

  component.onUserClick('123');

  expect(navigateSpy).toHaveBeenCalledWith(['/users', '123']);
});
```

## Next Steps

- See [Configuration](configuration.md) to customize generation
- Check [Troubleshooting](troubleshooting.md) for common issues
- Read [Contributing](contributing.md) to help improve ngt
