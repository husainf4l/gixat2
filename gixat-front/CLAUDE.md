# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Gixat Front is an Angular 20 application that serves as the frontend for the Gixat platform. The application uses Apollo GraphQL for backend communication, supports Google OAuth authentication, and includes organization management features.

## Development Commands

### Local Development
```bash
npm start                    # Start dev server on port 3002
ng serve --port 3002        # Explicit dev server command
npm run build               # Build for production
npm run watch               # Build with watch mode (development)
npm test                    # Run unit tests with Karma
```

### Code Generation
```bash
ng generate component component-name    # Generate new component
ng generate --help                      # View all available schematics
```

### Docker Development
```bash
docker-compose up           # Start containerized application
docker build -t gixat-front .    # Build Docker image locally
```

The Docker setup uses a multi-stage build with nginx serving the built Angular application. The `docker-entrypoint.sh` script generates runtime environment configuration at container startup.

## Architecture

### GraphQL Integration

The application uses Apollo Client for all backend communication. GraphQL queries and mutations are defined inline within services (not in separate `.graphql` files). All API calls go through the Apollo client configured in [src/app/graphql.provider.ts](src/app/graphql.provider.ts).

**Key configuration:**
- GraphQL endpoint: `${apiUrl}/graphql/`
- Credentials: `withCredentials: true` for cookie-based authentication
- Cache: `InMemoryCache`

### Authentication Flow

The authentication system supports both email/password and Google OAuth:

1. **Email/Password**: Login/register mutations via [AuthService](src/app/services/auth.service.ts)
2. **Google OAuth**: Uses `loginWithGoogle` mutation with Google ID tokens
3. **Session Management**: Cookie-based sessions with `withCredentials: true`
4. **Auth Guard**: [authGuard](src/app/auth.guard.ts) protects routes and enforces organization setup flow

**Critical auth guard logic:**
- Users without an organization are redirected to `/organization-setup`
- Users with an organization cannot access `/organization-setup`
- Unauthenticated users are redirected to `/auth/login`

### Organization Management

The application enforces a strict organization-based access model:

1. New users must either create or join an organization after registration
2. The `me` query returns user details including `organizationId` and nested `organization` object
3. Organization setup happens via `/organization-setup` route (protected by auth guard)
4. Joining organizations requires a valid invite code (validated via `inviteByCode` query)

### Routing Structure

Routes are defined in [src/app/app.routes.ts](src/app/app.routes.ts):
- Public routes: `/`, `/auth/login`, `/auth/register`, `/auth/callback`
- Protected routes: `/organization-setup`, `/dashboard/*`
- Dashboard uses `DashboardLayoutComponent` as a layout wrapper with child routes

### Environment Configuration

The application uses a runtime environment configuration system for Docker deployments:

**Development**: [src/environments/environment.ts](src/environments/environment.ts) reads from `window.__env` object
**Docker**: `docker-entrypoint.sh` generates `env-config.js` at container startup
**Environment variable**: `API_URL` controls the backend GraphQL endpoint

Default API URL: `http://localhost:8002`

### Styling

The project uses Tailwind CSS v4 with Angular-specific configurations:
- Tailwind PostCSS integration via `@tailwindcss/postcss`
- Main styles in [src/styles.css](src/styles.css)
- Prettier configured for 100-character line width with single quotes

## Testing

Unit tests use Jasmine + Karma:
- Test files: `*.spec.ts` alongside source files
- Configuration: `tsconfig.spec.json`
- Run with: `npm test`

## Backend Integration

The frontend expects a C# .NET GraphQL backend (located in `../GixatBackend`). Key integration points:

- Session-based authentication (cookies)
- GraphQL schema must match inline queries/mutations in services
- Backend runs on port 8002 by default
- CORS must allow credentials from frontend origin

### Common GraphQL Operations

**Authentication:**
- `login(input: LoginInput!)` - Returns user and error
- `register(input: RegisterInput!)` - Returns user and error
- `loginWithGoogle(idToken: String!)` - Google OAuth login
- `me` - Get current user details

**Organization:**
- `createOrganization(input: CreateOrganizationInput!)` - Create new org
- `inviteByCode(code: String!)` - Validate invite code
- `assignUserToOrganization(organizationId: UUID!, userId: String!)` - Join org

## Port Configuration

- Frontend dev server: `3002`
- Backend GraphQL API: `8002` (configured in environment)
- Production nginx: `80` (Docker container)

## Key Files to Understand

- [src/app/graphql.provider.ts](src/app/graphql.provider.ts) - Apollo Client setup
- [src/app/services/auth.service.ts](src/app/services/auth.service.ts) - All auth-related operations
- [src/app/auth.guard.ts](src/app/auth.guard.ts) - Route protection and org flow logic
- [src/app/app.routes.ts](src/app/app.routes.ts) - Application routing
- [docker-entrypoint.sh](docker-entrypoint.sh) - Runtime environment config generation
- [nginx.conf](nginx.conf) - Production nginx configuration with SPA routing
