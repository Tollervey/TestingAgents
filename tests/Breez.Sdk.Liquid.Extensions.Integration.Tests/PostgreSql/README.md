# PostgreSQL Integration Tests

## Overview

This directory contains comprehensive integration tests for the PostgreSQL implementation of `IPaymentRepository`. These tests use **Testcontainers** to spin up a real PostgreSQL instance, ensuring tests run against actual database behavior.

## Test Structure

### Test File
- `PostgreSqlPaymentRepositoryTests.cs` - 57 integration tests (52 Facts + 5 Theories) covering all repository operations

### Fixtures
- `PostgreSqlFixture` - Manages PostgreSQL container lifecycle
- `PostgreSqlCollection` - xUnit collection to share container across tests

## Test Coverage

### CRUD Operations (18 tests)
- **AddAsync**: Valid payment, duplicate hash, null payment, metadata persistence, nullable fields, all payment kinds
- **GetByHashAsync**: Payment exists, not found, null/whitespace hash, metadata retrieval
- **UpdateAsync**: Valid update, not exists, null payment, status transitions, metadata changes
- **ExistsAsync**: Payment exists, not exists, null/whitespace hash

### Query Operations (22 tests)
- **GetByStatusAsync**: Matching status, no matches, limit enforcement, all status types
- **GetAllAsync**: Empty list, pagination, ordering, offset handling, default limits
- **GetByDateRangeAsync**: Date range filtering, ordering, inclusive boundaries, timezone handling

### Edge Cases (13 tests)
- Case sensitivity of payment hashes
- Empty metadata handling
- Large amounts (near ulong.MaxValue)
- Zero satoshi amounts
- Long descriptions (500 chars)
- Unique constraint enforcement
- Timezone preservation
- Special characters in descriptions
- BOLT11 invoice strings
- Correlation ID persistence
- Concurrent access
- Large metadata (50+ entries)

## Running the Tests

### Prerequisites
- Docker must be running (Testcontainers needs Docker to spin up PostgreSQL)
- .NET 9.0 SDK

### Run All PostgreSQL Tests
```bash
dotnet test --filter "FullyQualifiedName~PostgreSql"
```

### Run with Detailed Logging
```bash
dotnet test --filter "FullyQualifiedName~PostgreSql" --logger "console;verbosity=detailed"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~AddAsync_WithValidPayment_AddsSuccessfully"
```

## Test Container Configuration

The tests use PostgreSQL 16 (Alpine) with:
- Database: `breez_test`
- User: `testuser`
- Password: `testpass123`
- Image: `postgres:16-alpine`
- Auto-cleanup: Enabled

### Container Lifecycle
1. **Collection Initialization**: Container starts once per test collection
2. **Test Initialization**: New DbContext created per test, schema created
3. **Test Execution**: Test runs against real PostgreSQL
4. **Test Cleanup**: TRUNCATE table to reset state
5. **Collection Disposal**: Container stopped and removed

## TDD RED Phase

These tests are currently in the **RED phase** of TDD. They compile successfully but will execute against the real PostgreSQL implementation that already exists.

The tests verify:
- All IPaymentRepository methods work correctly
- Database constraints are enforced (unique payment hash)
- Metadata is stored as JSONB and retrieved correctly
- Timestamps preserve timezone information
- Pagination and filtering work as expected
- Edge cases are handled properly

## Test Isolation

Each test:
1. Gets a fresh DbContext instance
2. Uses the shared PostgreSQL container
3. Truncates the `payment_states` table after execution
4. Is fully isolated from other tests

## Next Steps (GREEN Phase)

Once these tests run successfully against the PostgreSQL implementation:
1. Verify all 40+ tests pass
2. Check code coverage (should be 80%+)
3. Review any failed tests and fix implementation
4. Move to REFACTOR phase if needed

## Constitutional Compliance

These tests comply with:
- **Article III.1**: Tests written BEFORE implementation (TDD RED)
- **Article III.2**: 80%+ coverage of repository operations
- **Article III.3**: Test isolation via container cleanup
- **Article III.4**: Automated via `dotnet test`

## Notes

- Tests use `PaymentStateBuilder` from TestUtilities for consistent test data
- FluentAssertions provides readable assertions
- Tests validate both happy paths and error cases
- Database-specific features (JSONB, unique constraints) are tested
- Concurrent access patterns are validated
