# Supported File Types

ngt automatically detects and generates appropriate tests for various Angular file types. This guide details what gets generated for each type.

## Overview

| File Type | Naming Convention | Detection Method |
|-----------|-------------------|------------------|
| [Component](#components) | `*.component.ts` | File name suffix |
| [Service](#services) | `*.service.ts` | File name suffix |
| [Directive](#directives) | `*.directive.ts` | File name suffix |
| [Pipe](#pipes) | `*.pipe.ts` | File name suffix |
| [Guard](#guards) | `*.guard.ts` | File name suffix + content analysis |
| [Interceptor](#interceptors) | `*.interceptor.ts` | File name suffix + content analysis |
| [Resolver](#resolvers) | `*.resolver.ts` | File name suffix + content analysis |
| [Model](#models) | `*model*.ts`, `*interface*.ts` | File name contains keyword |
| [Module](#modules) | `*.module.ts` | File name suffix |

## Components

### Detection

Files ending with `.component.ts` are detected as components.

### Content Analysis

ngt analyzes component files for:
- **Standalone detection**: Checks for `standalone: true` in the decorator
- **Dependencies**: Detects services injected via `inject()`
- **Class name extraction**: Extracts the exported class name

### Generated Test Structure

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { UserComponent } from './user.component';

describe('UserComponent', () => {
  let component: UserComponent;
  let fixture: ComponentFixture<UserComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserComponent],  // For standalone components
      // declarations: [UserComponent],  // For module-based components
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserComponent);
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

### Features

- Automatic standalone vs module-based detection
- HTTP client mocking when HttpClient is detected
- Router mocking when Router is detected

## Services

### Detection

Files ending with `.service.ts` are detected as services.

### Content Analysis

ngt analyzes service files for:
- **Dependencies**: `HttpClient`, `Router`, `API_BASE_URL` injection tokens
- **Public methods**: Methods that return `Observable<T>` or `Promise<T>`
- **HTTP patterns**: GET, POST, PUT, DELETE methods

### Generated Test Structure

```typescript
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
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
});
```

### Features

- Automatic HTTP method detection based on method naming:
  - `get*`, `Get*` → GET
  - `create*`, `add*`, `login*`, `register*` → POST
  - `update*`, `Update*` → PUT
  - `delete*`, `remove*` → DELETE
- HttpTestingController integration
- API_BASE_URL injection token support
- Method parameter mocking

## Directives

### Detection

Files ending with `.directive.ts` are detected as directives.

### Generated Test Structure

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

### Features

- Generates test host component for directive testing
- Uses standalone component pattern
- DOM element selection for directive verification

## Pipes

### Detection

Files ending with `.pipe.ts` are detected as pipes.

### Generated Test Structure

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

### Features

- Direct instantiation (no TestBed required for pure pipes)
- Transform method testing

## Guards

### Detection

Files ending with `.guard.ts` are detected as guards.

### Content Analysis

ngt distinguishes between:
- **Functional guards**: Using `CanActivateFn` pattern
- **Class-based guards**: Using `@Injectable()` class pattern

### Functional Guard Test

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

### Features

- Functional vs class-based detection
- `TestBed.runInInjectionContext()` for functional guards
- AuthService mocking when detected
- Router navigation verification

## Interceptors

### Detection

Files ending with `.interceptor.ts` are detected as interceptors.

### Content Analysis

ngt distinguishes between:
- **Functional interceptors**: Using `HttpInterceptorFn` pattern
- **Class-based interceptors**: Using `@Injectable()` class pattern

### Functional Interceptor Test

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

### Features

- Functional vs class-based detection
- `withInterceptors()` configuration for functional interceptors
- AuthService mocking
- HTTP request/response verification

## Resolvers

### Detection

Files ending with `.resolver.ts` are detected as resolvers.

### Content Analysis

ngt distinguishes between:
- **Functional resolvers**: Using `ResolveFn<T>` pattern
- **Class-based resolvers**: Using `@Injectable()` class pattern

### Functional Resolver Test

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

### Features

- Functional vs class-based detection
- `TestBed.runInInjectionContext()` for functional resolvers

## Models

### Detection

Files containing `model` or `interface` in the name are detected as models.

### Content Analysis

ngt checks whether the file contains:
- **Classes**: Generates tests for class instantiation
- **Interfaces/Types only**: Skips test generation (no runtime code)

### Generated Test Structure (for classes)

```typescript
import { User } from './user.model';

describe('User', () => {
  it('should be defined', () => {
    expect(User).toBeDefined();
  });

  it('should create instance', () => {
    const instance = new User();
    expect(instance).toBeTruthy();
  });
});
```

### Skipped Files

Files that only export interfaces or type aliases are skipped:

```
- Skipped: user.model (interface/type only)
```

## Modules

### Detection

Files ending with `.module.ts` are detected as modules.

### Generated Test Structure

Module testing is minimal since Angular modules are primarily configuration:

```typescript
import { AppModule } from './app.module';

describe('AppModule', () => {
  it('should be defined', () => {
    expect(AppModule).toBeDefined();
  });
});
```

## Dependency Detection

ngt automatically detects common Angular dependencies and adds appropriate mocks:

| Dependency | Detection | Mock Provided |
|------------|-----------|---------------|
| `HttpClient` | `inject(HttpClient)` | `provideHttpClient()`, `provideHttpClientTesting()` |
| `Router` | `inject(Router)` | `provideRouter([])` |
| `AuthService` | `inject(AuthService)` | Vitest mock object |
| `API_BASE_URL` | `inject(API_BASE_URL)` | `{ provide: API_BASE_URL, useValue: '...' }` |

## Vitest vs Jest

Generated tests use **Vitest** syntax for mocking (`vi.fn()`, `vi.spyOn()`, `Mock` type). If your project uses Jest, you may need to:

1. Replace `vi.fn()` with `jest.fn()`
2. Replace `vi.spyOn()` with `jest.spyOn()`
3. Update import from `import { vi, Mock } from 'vitest'` to Jest equivalents

## Next Steps

- View [Examples](examples.md) for complete test file examples
- See [Configuration](configuration.md) to customize generation
- Check [Troubleshooting](troubleshooting.md) for common issues
