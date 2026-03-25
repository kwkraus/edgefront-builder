# Registration File Processing with AI-Driven Parsing

## Overview
Implement a complete backend infrastructure for registration file processing using Azure AI Foundry Projects API. This includes schema updates, new DTOs, service layers for AI parsing, file handling, and two new endpoints (preview + confirm) for a two-step import workflow.

## Architecture Approach
- **Database Schema**: Extend NormalizedRegistration entity with FirstName and LastName properties
- **AI Parsing Service**: Stateless service that calls Azure AI Foundry REST API with structured prompt
- **File Parser Abstraction**: Interface + implementation (CsvRegistrationFileParser) for reading form files
- **Two-Step Workflow**:
  1. **Preview**: Parse CSV without persisting; return parsed registrants with validation results
  2. **Confirm**: Validate, persist, and recompute metrics atomically
- **Error Handling**: Partial success model (return successfully parsed + failed rows with error reasons)

## Implementation Plan (Sequential)

### Phase 1: Database & Entity Schema (2 tasks)
1. **Update NormalizedRegistration entity**
   - File: `src/backend/Domain/Entities/NormalizedRegistration.cs`
   - Add: `public string? FirstName { get; set; }`
   - Add: `public string? LastName { get; set; }`
   - Keep existing: Email, EmailDomain, RegisteredAt, SessionId, OwnerUserId, RegistrationId

2. **Create EF Core migration**
   - Run: `dotnet ef migrations add AddNameFieldsToNormalizedRegistration --project src/backend/EdgeFront.Builder.Api.csproj --startup-project src/backend/EdgeFront.Builder.Api.csproj`
   - Verify: Two nullable string columns added to NormalizedRegistrations table

### Phase 2: DTOs & Data Contracts (3 tasks)
3. **Create ParsedRegistrant DTO**
   - File: `src/backend/Features/Sessions/Dtos/ParsedRegistrant.cs`
   - Properties:
     - `Email: string` (required)
     - `FirstName: string` (required)
     - `LastName: string` (required)
     - `RegisteredAt: DateTime`
     - `Status: string` ("success" or "failed")
     - `ErrorReason: string?` (only if Status = "failed")

4. **Create RegistrationPreviewDto**
   - File: `src/backend/Features/Sessions/Dtos/RegistrationPreviewDto.cs`
   - Properties:
     - `SessionTitle: string`
     - `RegistrantCount: int`
     - `SuccessCount: int`
     - `FailedCount: int`
     - `Registrants: List<ParsedRegistrant>`
     - `Warnings: List<string>?` (non-blocking)
     - `Errors: List<string>?` (blocking)

5. **Create ConfirmRegistrationImportRequest DTO**
   - File: `src/backend/Features/Sessions/Dtos/ConfirmRegistrationImportRequest.cs`
   - Properties:
     - `Registrants: List<ParsedRegistrant>`

### Phase 3: Service Layer (2 tasks)
6. **Create RegistrationParsingService**
   - File: `src/backend/Features/Sessions/RegistrationParsingService.cs`
   - Class: `public class RegistrationParsingService`
   - Constructor: DI inject `IHttpClientFactory` and `IConfiguration`
   - Configuration keys:
     - `AzureAI:Endpoint` (project endpoint)
     - `AzureAI:ApiKey` (API key)
     - `AzureAI:ProjectName` (project name/ID)
   - Public method:
     ```csharp
     public async Task<List<ParsedRegistrant>> ParseRegistrationsAsync(
         string csvText, 
         CancellationToken ct = default)
     ```
   - Implementation:
     - Call Azure AI Foundry REST API with structured prompt
     - Prompt: "Extract registrant information from this CSV text. For each row, extract: email (required), firstName (required, apply title case), lastName (required, apply title case), registeredAt (optional, UTC datetime). If any field is missing or unparseable, mark status as 'failed' with error reason. Return JSON array: [{email, firstName, lastName, registeredAt, status, errorReason}]"
     - Deserialize JSON response
     - Handle partial success (return both successful + failed rows)
     - Wrap API errors in clear exception message

7. **Create IRegistrationFileParser interface & CsvRegistrationFileParser**
   - Files:
     - `src/backend/Features/Sessions/IRegistrationFileParser.cs`
     - `src/backend/Features/Sessions/CsvRegistrationFileParser.cs`
   - Interface: 
     ```csharp
     public interface IRegistrationFileParser
     {
         Task<List<ParsedRegistrant>> ParseAsync(IFormFile file, CancellationToken ct);
     }
     ```
   - Implementation:
     - Read IFormFile as text using StreamReader
     - Call `RegistrationParsingService.ParseRegistrationsAsync(csvText, ct)`
     - Return parsed registrants

### Phase 4: DI Configuration (1 task)
8. **Register services in Program.cs**
   - Register `RegistrationParsingService` (scoped)
   - Register `IRegistrationFileParser` → `CsvRegistrationFileParser` (scoped)
   - Add configuration binding for AzureAI section with validation

### Phase 5: Endpoints (2 tasks)
9. **Create Preview Endpoint**
   - Endpoint: `POST /api/v1/sessions/{id:guid}/imports/registrations/preview`
   - Route group: `/api/v1/sessions/{id:guid}`
   - Logic:
     - Extract sessionId from route, userId from HttpContext
     - Accept IFormFile from form data
     - Validate file (CSV, not empty)
     - Get session from DB and verify ownership
     - Call `IRegistrationFileParser.ParseAsync(file, ct)` to get parsed registrants
     - Extract session title from CSV (look for "Meeting title" field or first row)
     - Build `RegistrationPreviewDto` with counts and parsed registrants
     - Return DTO (do NOT save to database)
   - Error handling:
     - 401 Unauthorized if userId is null
     - 404 Not Found if session not found or not owned
     - 400 Bad Request if file is invalid/empty
   - Success: 200 OK with RegistrationPreviewDto

10. **Create Confirm Endpoint**
    - Endpoint: `POST /api/v1/sessions/{id:guid}/imports/registrations/confirm`
    - Route group: `/api/v1/sessions/{id:guid}`
    - Request body: `ConfirmRegistrationImportRequest` with registrants list
    - Logic:
      - Extract userId from HttpContext
      - Validate all registrants (email format, names not empty, registeredAt valid)
      - Get session from DB and verify ownership
      - Begin transaction:
        - Delete existing registrations for this session
        - Insert all new registrations (with firstName, lastName)
        - Update/insert SessionImportSummary
        - Call MetricsRecomputeService to recompute session and series metrics
      - Commit transaction
      - Return SessionImportSummaryDto
    - Error handling:
      - 401 Unauthorized if userId is null
      - 404 Not Found if session not found or not owned
      - 400 Bad Request if validation fails
    - Success: 200 OK with SessionImportSummaryDto

### Phase 6: Configuration & Testing (2 tasks)
11. **Update appsettings.json**
    - Add AzureAI section with placeholder values:
      ```json
      "AzureAI": {
        "Endpoint": "https://your-project.cognitiveservices.azure.com/",
        "ApiKey": "your-api-key",
        "ProjectName": "your-project-name"
      }
      ```

12. **Write Unit & Integration Tests**
    - Test RegistrationParsingService with mock HTTP responses
    - Test CsvRegistrationFileParser with sample CSV files
    - Test preview endpoint with valid/invalid registrations
    - Test confirm endpoint with persistence and metrics recompute
    - Test error cases: malformed CSV, missing fields, API failures

## Key Decisions
1. **REST API over SDK**: Use Azure AI Foundry REST API directly for simplicity
2. **Stateless parsing service**: No caching; each parse is independent
3. **Partial success model**: Failed rows are returned as part of response, not thrown
4. **Two-step workflow**: Preview (no DB write) + Confirm (atomic transaction) for user safety
5. **Name normalization**: Handled by AI prompt (title case applied by AI, not post-processing)
6. **Configuration validation**: AzureAI section validated on startup in Program.cs

## Files to Create/Modify
- **Create** (7 new files):
  - `src/backend/Features/Sessions/Dtos/ParsedRegistrant.cs`
  - `src/backend/Features/Sessions/Dtos/RegistrationPreviewDto.cs`
  - `src/backend/Features/Sessions/Dtos/ConfirmRegistrationImportRequest.cs`
  - `src/backend/Features/Sessions/RegistrationParsingService.cs`
  - `src/backend/Features/Sessions/IRegistrationFileParser.cs`
  - `src/backend/Features/Sessions/CsvRegistrationFileParser.cs`
  - `src/backend/Migrations/[timestamp]_AddNameFieldsToNormalizedRegistration.cs`

- **Modify** (3 existing files):
  - `src/backend/Domain/Entities/NormalizedRegistration.cs` (add properties)
  - `src/backend/Program.cs` (register services + configuration)
  - `src/backend/appsettings.json` (add AzureAI section)
  - `src/backend/Features/Sessions/SessionEndpoints.cs` (add preview + confirm endpoints)

- **Create** (tests):
  - `tests/backend/EdgeFront.Builder.Tests/Features/Sessions/RegistrationParsingServiceTests.cs`
  - `tests/backend/EdgeFront.Builder.Tests/Features/Sessions/CsvRegistrationFileParserTests.cs`
  - `tests/backend/EdgeFront.Builder.Tests/Features/Sessions/RegistrationPreviewEndpointTests.cs`

## Validation Steps
1. Build backend: `dotnet build src/backend/EdgeFront.Builder.Api.csproj`
2. Run unit tests: `dotnet test tests/backend/EdgeFront.Builder.Tests.csproj`
3. Verify migration: Inspect generated migration file
4. Manual endpoint testing via Swagger or Postman

## Dependencies & Assumptions
- Azure AI Foundry project already provisioned (credentials provided via configuration)
- HttpClientFactory is available in DI container (standard ASP.NET Core)
- MetricsRecomputeService exists and is injectable (already in codebase)
- EF Core migrations work with existing database context
- User context extraction (`HttpContext.GetUserOid()`) works as expected

## Out of Scope
- Frontend upload component (parallel work)
- Detailed structured logging (can add later)
- Pagination of results (preview returns all)
- Batch import (single file per session at a time)
- Retry logic for transient AI API failures (can add later)
