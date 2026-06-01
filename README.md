# JWT Auth System

A standalone authentication and authorization service built with ASP.NET Core 8.

## Features

- **User Management**: Secure registration and login.
- **JWT Authentication**: Short-lived access tokens (15 min).
- **Refresh Token Rotation**: Long-lived refresh tokens (7 days) with rotation on every use.
- **Role-Based Authorization**: `Admin` and `User` roles with policy-based access control.
- **Security**: Password hashing with BCrypt (work factor 12), token reuse detection, and rate limiting.
- **Global Error Handling**: Consistent API responses for errors.
- **Swagger Documentation**: Interactive API documentation with Bearer token support.

## Tech Stack

- **Framework**: ASP.NET Core 8
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 8
- **Validation**: FluentValidation
- **Testing**: xUnit, Moq, FluentAssertions

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)

## Getting Started

1. **Clone the repository**:
   ```bash
   git clone <repo-url>
   cd jwt-auth-system
   ```

2. **Set up environment variables**:
   Create a `.env` file in the root directory (already provided in this setup):
   ```env
   POSTGRES_USER=postgres
   POSTGRES_PASSWORD=postgres
   POSTGRES_DB=auth_db
   JWT_SECRET_KEY=your_very_secure_secret_key_at_least_256_bits
   ```

3. **Start the database**:
   ```bash
   docker-compose up -d
   ```

4. **Run migrations**:
   ```bash
   dotnet ef database update --project AuthSystem.Infrastructure --startup-project AuthSystem.Api
   ```

5. **Run the application**:
   ```bash
   dotnet run --project AuthSystem.Api
   ```

The API will be available at `https://localhost:5001` (or the port specified in your `launchSettings.json`).
Swagger UI: `https://localhost:5001/swagger`

## API Usage Examples

### Register a User
```bash
curl -X POST https://localhost:5001/api/v1/Auth/register \
-H "Content-Type: application/json" \
-d '{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}'
```

### Login
```bash
curl -X POST https://localhost:5001/api/v1/Auth/login \
-H "Content-Type: application/json" \
-d '{
  "email": "user@example.com",
  "password": "SecurePass123!"
}'
```

### Refresh Token
```bash
curl -X POST https://localhost:5001/api/v1/Auth/refresh \
-H "Content-Type: application/json" \
-d '{
  "refreshToken": "your_refresh_token_here"
}'
```

## Security Design Decisions

- **BCrypt**: Used for password hashing with a work factor of 12 to balance security and performance.
- **Token Rotation**: Every time a refresh token is used, it's revoked and a new one is issued. This helps detect stolen tokens.
- **Reuse Detection**: If a revoked refresh token is presented, the system revokes ALL active tokens for that user as a precaution.
- **Rate Limiting**: Protected `/auth` endpoints against brute force attacks (5 requests per minute).
- **Generic Auth Errors**: The system returns generic "Email or password is incorrect" messages to prevent user enumeration.

## Running Tests

```bash
dotnet test
```
