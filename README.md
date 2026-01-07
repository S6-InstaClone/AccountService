# AccountService

A microservice for managing user accounts and profiles in the InstaClone application. Handles user profile management, Keycloak integration, and GDPR-compliant account deletion.

## Overview

AccountService is part of the InstaClone microservices architecture, responsible for:
- User profile management (CRUD operations)
- Integration with Keycloak for identity management
- Publishing account deletion events for GDPR compliance
- Coordinating cross-service data cleanup

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 8.0 |
| Database | PostgreSQL 9.x |
| ORM | Entity Framework Core 9.x |
| Identity Provider | Keycloak |
| Message Broker | RabbitMQ (via MassTransit) |
| Blob Storage | Azure Blob Storage |
| Monitoring | OpenTelemetry + Prometheus |
| Containerization | Docker |

## API Endpoints

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| GET | `/api/Account/me` | Yes | Get current user's account info |
| DELETE | `/api/Account/delete-me` | Yes | Delete current user's account (GDPR) |
| GET | `/api/Profile/{id}` | Yes | Get a user profile |
| POST | `/api/Profile` | Yes | Create a profile |
| PUT | `/api/Profile/{id}` | Yes | Update a profile |

## Project Structure

```
AccountService/
├── Business/
│   ├── KeycloakService.cs        # Keycloak admin API integration
│   ├── ProfileService.cs         # Profile business logic
│   └── Converters/
├── Controllers/
│   └── ProfileController.cs      # REST API endpoints (AccountController)
├── Dtos/
│   ├── CreateProfileDto.cs
│   ├── UpdateProfileNameDto.cs
│   ├── UpdateProfileDescDto.cs
│   ├── UploadProfilePictureDto.cs
│   └── SearchProfileDto.cs
├── Messages/
│   └── AccountDeletedEvent.cs    # Event contract for GDPR
├── Models/
│   ├── AccountData.cs
│   └── Profile.cs
├── Persistence/
│   ├── AccountRepository.cs      # DbContext
│   └── BlobService.cs
├── Program.cs                    # Application entry point
└── appsettings.json
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DB_HOST` | PostgreSQL host | `account-db` |
| `DB_PORT` | PostgreSQL port | `5432` |
| `DB_NAME` | Database name | `accounts` |
| `DB_USER` | Database username | Required |
| `DB_PASSWORD` | Database password | Required |
| `RABBITMQ_HOST` | RabbitMQ host | `rabbitmq` |
| `RABBITMQ_USER` | RabbitMQ username | Required |
| `RABBITMQ_PASSWORD` | RabbitMQ password | Required |
| `KEYCLOAK_ADMIN_URL` | Keycloak base URL | `http://keycloak:8080` |
| `KEYCLOAK_REALM` | Keycloak realm name | `instaclone` |
| `KEYCLOAK_ADMIN_CLIENT_ID` | Admin client ID | `admin-cli` |
| `KEYCLOAK_ADMIN_USERNAME` | Admin username | Required |
| `KEYCLOAK_ADMIN_PASSWORD` | Admin password | Required |

### Example `.env` File

```bash
DB_HOST=account-db
DB_PORT=5432
DB_NAME=accounts
DB_USER=admin
DB_PASSWORD=your_secure_password

RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=your_secure_password

KEYCLOAK_ADMIN_URL=http://keycloak:8080
KEYCLOAK_REALM=instaclone
KEYCLOAK_ADMIN_CLIENT_ID=admin-cli
KEYCLOAK_ADMIN_USERNAME=admin
KEYCLOAK_ADMIN_PASSWORD=your_secure_password

ASPNETCORE_ENVIRONMENT=Docker
```

## Running Locally

### Prerequisites
- .NET 8.0 SDK
- Docker (for PostgreSQL, RabbitMQ, Keycloak)

### Development Setup

```bash
# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update

# Run the service
dotnet run
```

### Docker

```bash
# Build image
docker build -t accountservice .

# Run container
docker run -p 5005:5005 --env-file .env accountservice
```

## Testing

```bash
# Run all tests
cd Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~AccountControllerTests"
```

### Test Coverage

The test suite covers:
- Account controller endpoints
- Profile service operations
- Keycloak service integration
- Model and DTO validation
- GDPR deletion flow

## GDPR Compliance

The `/api/Account/delete-me` endpoint implements GDPR-compliant account deletion:

1. **Keycloak Deletion**: User is removed from Keycloak identity provider
2. **Event Publishing**: `AccountDeletedEvent` is published to RabbitMQ
3. **Cross-Service Cleanup**: Other services (PostService, etc.) receive the event and delete related data

### AccountDeletedEvent Schema

```csharp
public record AccountDeletedEvent
{
    public string UserId { get; init; }      // Keycloak UUID
    public string? Username { get; init; }
    public string? Email { get; init; }
    public DateTime DeletedAt { get; init; }
    public string Reason { get; init; }      // "GDPR_USER_REQUEST" or "ADMIN_ACTION"
}
```

## Keycloak Integration

AccountService integrates with Keycloak for:
- User deletion via Admin REST API
- Token acquisition for admin operations

The service uses the master realm to obtain admin tokens and performs operations on the configured realm.

## Monitoring

Prometheus metrics are exposed at `/metrics`:
- ASP.NET Core request metrics
- HTTP client metrics
- .NET runtime metrics (GC, memory, thread pool)

## CI/CD

GitHub Actions workflows:
- **ci-security.yml**: Build, test, and security scanning
- **deploy.yml**: Build and push Docker image to GHCR
- **deploy-security.yml**: Security-focused deployment with Trivy scanning
- **sonarqube.yml**: SonarCloud SAST analysis

## Security Features

- Gitleaks for secret detection
- SonarCloud for SAST analysis
- Trivy for container vulnerability scanning
- Dependency vulnerability checking
- Environment variable-based secrets management

## Architecture Notes

- AccountService acts as the coordinator for account deletion
- Authentication is handled by the API Gateway; services receive user info via headers
- Profile data is stored locally; authentication data is in Keycloak

## License

Proprietary - Fontys ICT Advanced Software Project
