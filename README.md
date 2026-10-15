# VexCash MessageBird SMS Service

A modern REST API microservice built with C# / .NET 10 for automated loan notifications, payment reminders, late payment notices (*Mahnung*), and contract terminations (*Kündigung*) via the **MessageBird SMS API**.

---

## Table of Contents
1. [Overview & Requirements](#overview--requirements)
2. [Architecture & Technologies](#architecture--technologies)
3. [Running with Docker (Recommended for Evaluators)](#running-with-docker-recommended-for-evaluators)
4. [Running Locally with .NET SDK](#running-locally-with-net-sdk)
5. [Data Model & In-Memory Database](#data-model--in-memory-database)
6. [MessageBird Integration & Simulation Mode](#messagebird-integration--simulation-mode)
7. [API Endpoints](#api-endpoints)
8. [Automated Tests](#automated-tests)
9. [Sample Requests (cURL & .http)](#sample-requests-curl--http)

---

## Overview & Requirements

| Requirement | Status | Implementation Details |
| :--- | :---: | :--- |
| **REST API Service in C# / .NET** | ✅ | Built with ASP.NET Core (.NET 10), Controller-based routing, Repository pattern, and OpenAPI / Swagger UI. |
| **In-Memory Database Mock** | ✅ | EF Core In-Memory Database (`Microsoft.EntityFrameworkCore.InMemory`) with automatic database seeding on startup. |
| **Customer Information (Table 1)** | ✅ | `Customer` entity containing `phonenr`, `name`, `creditnumber`, `amount`, and `dueDate`. |
| **Message Templates (Table 2)** | ✅ | `MessageTemplate` entity supporting templates for **Erinnerung** (Reminder), **Mahnung** (Dunning notice), and **Kuendigung** (Termination). |
| **Placeholder Replacement** | ✅ | Dynamic token substitution for `{Creditnumber}`, `{Name}`, `{dueDate}`, and `{amount}` (case-insensitive, localized German currency & date formatting). |
| **MessageBird SMS API** | ✅ | Asynchronous HTTP client targeting `https://rest.messagebird.com/messages` with MSISDN normalization and automatic fallback to **Simulation Mode** when no live credentials are provided. |
| **Clean Architecture & Decoupling** | ✅ | Strict Repository Layer (`ICustomerRepository`, `ITemplateRepository`, `ISmsLogRepository`) separating persistence from business logic and controllers. |
| **Extended Features** | ✅ | SMS preview (`/api/sms/preview`), bulk dispatch for overdue loans (`/api/sms/send-bulk`), and an audit trail history (`/api/sms/history`). |

---

## Architecture & Technologies

- **Runtime:** .NET 10.0 SDK (`net10.0`)
- **Web Framework:** ASP.NET Core Web API
- **Persistence:** Entity Framework Core (In-Memory provider)
- **Architecture:** Controller -> Service -> Repository Layer
- **SMS Gateway:** MessageBird REST API with MSISDN normalization
- **API Documentation:** Swashbuckle / Swagger UI with XML documentation comments
- **Containerization:** Multi-stage Docker build with Microsoft .NET 10 Alpine/Debian base images
- **Testing:** xUnit, FluentAssertions, Moq, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)

### Project Structure

```
VexCashTestTask/
├── compose.yaml                            # Docker Compose orchestration (test runner + API)
├── .dockerignore
├── VexCash.SmsService.sln
├── src/
│   └── VexCash.SmsService/
│       ├── Dockerfile                      # Multi-stage production Dockerfile for the API
│       ├── Controllers/
│       │   ├── CustomersController.cs      # Customer CRUD via ICustomerRepository
│       │   ├── TemplatesController.cs      # Template management via ITemplateRepository
│       │   └── SmsController.cs            # Dispatch, Preview & History via Repositories
│       ├── Data/
│       │   ├── AppDbContext.cs             # EF Core In-Memory DbContext
│       │   └── DbInitializer.cs            # Seeds sample customers & German templates
│       ├── Repositories/
│       │   ├── ICustomerRepository.cs & CustomerRepository.cs
│       │   ├── ITemplateRepository.cs & TemplateRepository.cs
│       │   └── ISmsLogRepository.cs & SmsLogRepository.cs
│       ├── Dtos/
│       │   ├── CustomerDtos.cs
│       │   ├── TemplateDtos.cs
│       │   └── SmsDtos.cs
│       ├── Models/
│       │   ├── Customer.cs
│       │   ├── MessageTemplate.cs
│       │   ├── SmsLog.cs
│       │   └── TemplateType.cs             # Erinnerung, Mahnung, Kuendigung
│       ├── Services/
│       │   ├── ITemplateRenderer.cs        # Placeholder replacement engine
│       │   ├── TemplateRenderer.cs
│       │   ├── IMessageBirdClient.cs       # MessageBird API HTTP Client
│       │   ├── MessageBirdClient.cs
│       │   ├── ISmsDispatchService.cs      # Core business logic & audit logging
│       │   └── SmsDispatchService.cs
│       ├── Options/
│       │   └── MessageBirdOptions.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Program.cs
│       ├── Startup.cs
│       └── VexCash.SmsService.http         # Ready-to-run HTTP test requests
└── tests/
    └── VexCash.SmsService.Tests/
        ├── Dockerfile                      # Automated test runner Dockerfile
        ├── RepositoryTests.cs              # Unit tests for repository layer
        ├── TemplateRendererTests.cs        # Tests for placeholder parsing & formatting
        ├── SmsDispatchServiceTests.cs      # Tests for single & bulk dispatch rules
        ├── MessageBirdClientTests.cs       # Tests for API client & payload validation
        └── IntegrationTests.cs             # End-to-end HTTP integration tests
```

---

## Running with Docker (Recommended for Evaluators)

The solution provides modular Dockerfiles for both the API service ([`src/VexCash.SmsService/Dockerfile`](file:///Users/miha/Projects/VexCashTestTask/src/VexCash.SmsService/Dockerfile)) and automated test suite ([`tests/VexCash.SmsService.Tests/Dockerfile`](file:///Users/miha/Projects/VexCashTestTask/tests/VexCash.SmsService.Tests/Dockerfile)), orchestrated via [`compose.yaml`](file:///Users/miha/Projects/VexCashTestTask/compose.yaml).

### Option A: Using Docker Compose (All-in-One: Automated Tests + API)

Docker Compose ensures the entire test suite passes before bringing up the API container:

```bash
docker compose up --build
```

- First, the `vexcash_tests` container builds and runs all unit & integration tests.
- Once tests pass successfully (`condition: service_completed_successfully`), the `vexcash_api` container is automatically started on port `8080`.
- Access Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)

To stop the containers:
```bash
docker compose down
```

---

### Option B: Build and Run via Docker CLI

1. **Build the API Docker Image:**
   ```bash
   docker build -f src/VexCash.SmsService/Dockerfile -t vexcash.smsservice .
   ```

2. **Run the Container:**
   ```bash
   docker run -d --name vexcash_api -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development vexcash.smsservice
   ```

   *(Optional)* If you want to supply a real MessageBird API key:
   ```bash
   docker run -d --name vexcash_api -p 8080:8080 \
     -e ASPNETCORE_ENVIRONMENT=Development \
     -e MessageBird__ApiKey="YOUR_ACTUAL_API_KEY" \
     vexcash.smsservice
   ```

3. **Open the Service:**
   - Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)
   - The root URL [http://localhost:8080/](http://localhost:8080/) automatically redirects to Swagger UI.

4. *(Optional)* Run tests via Docker CLI:
   ```bash
   docker build -f tests/VexCash.SmsService.Tests/Dockerfile -t vexcash.smsservice.tests .
   docker run --rm --name vexcash_tests vexcash.smsservice.tests
   ```

5. **Stop the Container:**
   ```bash
   docker stop vexcash_api && docker rm vexcash_api
   ```

---

### Option C: Running from a Standalone Docker Image (`.tar` / `.tar.gz` Archive)

If you received this project as a pre-packaged Docker archive (`vexcash_service.tar.gz` or `vexcash.smsservice.tar`) without source code:

1. **Load the Image into Docker:**
   ```bash
   docker load < vexcash_service.tar.gz
   ```
   *(or `docker load -i vexcash.smsservice.tar`)*

2. **Run the Container:**
   ```bash
   docker run -d --name vexcash_api -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development vexcash.smsservice
   ```

3. **Open the Service:**
   - Open your browser to [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## Running Locally with .NET SDK

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Run the Application

```bash
dotnet run --project src/VexCash.SmsService
```

The application will bind to:
- **HTTP:** `http://localhost:5066`
- **HTTPS:** `https://localhost:7044`
- **Swagger Documentation:** [http://localhost:5066/swagger](http://localhost:5066/swagger)

### 2. Run Automated Tests

```bash
dotnet test
```

All 26 unit and integration tests will execute automatically.

---

## Data Model & In-Memory Database

### 1. Customer Entity (`Customers` table)
- `id` (int, Primary Key)
- `phonenr` (string, e.g. `+491512345678`)
- `name` (string, e.g. `Max Mustermann`)
- `creditnumber` (string, e.g. `CR-2026-1001`)
- `amount` (decimal, e.g. `250.00`)
- `dueDate` (DateTime, e.g. `2026-10-20`)

### 2. Message Template Entity (`Templates` table)
- `id` (int, Primary Key)
- `type` (`Erinnerung`, `Mahnung`, `Kuendigung`)
- `title` (string)
- `content` (string with `{Creditnumber}`, `{Name}`, `{dueDate}`, and `{amount}` placeholders)

#### Default Pre-Seeded Templates:
1. **Erinnerung (Reminder):**
   ```
   {Creditnumber}

   Sehr geehrte/r {Name}
   Bitte zahlen sie bis zum {dueDate}
   Die Summe {amount} zurück
   ```
2. **Mahnung (Dunning Notice):**
   ```
   Mahnung: Kredit {Creditnumber}

   Sehr geehrte/r {Name},
   die Rate in Höhe von {amount} war am {dueDate} fällig. Bitte begleichen Sie den Betrag umgehend.
   ```
3. **Kündigung (Termination):**
   ```
   Kündigung: Kredit {Creditnumber}

   Sehr geehrte/r {Name},
   da die Summe von {amount} (Fälligkeit {dueDate}) nicht eingegangen ist, wird der Kreditvertrag gekündigt.
   ```

### 3. SMS Audit Log (`SmsLogs` table)
- `id`, `customerId`, `recipientPhoneNumber`, `recipientName`, `creditNumber`, `templateType`, `renderedMessage`, `sentAt`, `status`, `messageBirdId`, `errorMessage`.

---

## MessageBird Integration & Simulation Mode

Configuration is managed in [`appsettings.json`](file:///Users/miha/Projects/VexCashTestTask/src/VexCash.SmsService/appsettings.json) or via environment variables:

```json
"MessageBird": {
  "ApiKey": "YOUR_MESSAGEBIRD_API_KEY",
  "Originator": "VexCash",
  "SimulationMode": false
}
```

- **Seamless Out-of-the-Box Simulation Mode:** When the `ApiKey` is empty or set to `YOUR_MESSAGEBIRD_API_KEY`, the service automatically activates **Simulation Mode**. No external HTTP calls are dispatched, avoiding failed requests or charges. A simulated MessageBird ID (`sim_...`) is generated, logged, and stored in the audit trail. This allows anyone (such as reviewers and evaluators) to immediately test the application end-to-end without needing MessageBird credentials.
- **Live / Test Account Support:** If an active MessageBird API key (Live or Test Key) is provided, the service formats the payload adhering to the MessageBird REST API specification:
  - Phone numbers are automatically sanitized and normalized to MSISDN format (e.g. `491512345678`).
  - Only the required numeric recipient ID is transmitted in the `recipients` array.

---

## API Endpoints

### Customers (`/api/customers`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/customers` | Get all customers (supports optional `?overdueOnly=true` filter). |
| `GET` | `/api/customers/{id}` | Get customer by ID. |
| `POST` | `/api/customers` | Create a new customer record. |
| `PUT` | `/api/customers/{id}` | Update existing customer data. |
| `DELETE` | `/api/customers/{id}` | Remove a customer. |

### Templates (`/api/templates`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/templates` | Retrieve all message templates. |
| `GET` | `/api/templates/{type}` | Retrieve template by type (`Erinnerung`, `Mahnung`, `Kuendigung`). |
| `PUT` | `/api/templates/{type}` | Update template text. |

### SMS Dispatching (`/api/sms`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/sms/preview` | Preview rendered SMS text with populated customer placeholders without dispatching. |
| `POST` | `/api/sms/send` | Render and send SMS for a specific customer and template type. |
| `POST` | `/api/sms/send-bulk` | Bulk dispatch SMS to all customers (or overdue only). |
| `GET` | `/api/sms/history` | Retrieve full history/audit trail of dispatched SMS messages. |

---

## Automated Tests

The solution features a test suite covering unit logic and end-to-end API integration tests:

```bash
dotnet test
```

### Test Coverage Highlights:
- **`TemplateRendererTests`**: Validates placeholder substitution, case-insensitivity, currency formatting (`250,00 EUR`), and date formatting (`20.10.2026`).
- **`RepositoryTests`**: Validates EF Core In-Memory database CRUD and query operations across customer, template, and audit log repositories.
- **`MessageBirdClientTests`**: Validates MSISDN phone normalization, payload generation, and simulation handling.
- **`SmsDispatchServiceTests`**: Validates single dispatch, bulk overdue filtering, error handling, and audit log generation using repository mocks.
- **`IntegrationTests`**: End-to-end API testing using `WebApplicationFactory<Startup>`, verifying HTTP endpoints, Swagger availability, and database state transitions.

---

## Sample Requests (cURL & .http)

You can interact with the service using [Swagger UI](http://localhost:8080/swagger) (or `http://localhost:5066/swagger` if running locally via `dotnet run`), the included [`VexCash.SmsService.http`](file:///Users/miha/Projects/VexCashTestTask/src/VexCash.SmsService/VexCash.SmsService.http) file in Rider / VS Code, or directly via `curl`:

### 1. Preview Rendered SMS
```bash
curl -X POST http://localhost:8080/api/sms/preview \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "templateType": "Erinnerung"
  }'
```

**Response:**
```json
{
  "customerId": 1,
  "customerName": "Max Mustermann",
  "phoneNumber": "+491512345678",
  "creditNumber": "CR-2026-1001",
  "templateType": "Erinnerung",
  "renderedMessage": "CR-2026-1001\n\nSehr geehrte/r Max Mustermann\nBitte zahlen sie bis zum 20.10.2026\nDie Summe 250,00 EUR zurück",
  "characterCount": 110
}
```

### 2. Send SMS to a Customer
```bash
curl -X POST http://localhost:8080/api/sms/send \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "templateType": "Erinnerung"
  }'
```

**Response:**
```json
{
  "success": true,
  "status": "Simulated",
  "customerId": 1,
  "customerName": "Max Mustermann",
  "recipientPhoneNumber": "+491512345678",
  "templateType": "Erinnerung",
  "renderedMessage": "CR-2026-1001\n\nSehr geehrte/r Max Mustermann\nBitte zahlen sie bis zum 20.10.2026\nDie Summe 250,00 EUR zurück",
  "messageBirdId": "sim_8f1618a8fcde434ba3d5204ec17bf8f5",
  "timestamp": "2026-10-15T15:30:00Z"
}
```

### 3. Bulk Dispatch to Overdue Loans
```bash
curl -X POST http://localhost:8080/api/sms/send-bulk \
  -H "Content-Type: application/json" \
  -d '{
    "templateType": "Mahnung",
    "onlyOverdue": true
  }'
```

### 4. View SMS History / Audit Log
```bash
curl http://localhost:8080/api/sms/history
```
