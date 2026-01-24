<!--
================================================================================
SYNC IMPACT REPORT
================================================================================
Version change: 0.0.0 → 1.0.0 (MAJOR - initial ratification)

Added sections:
  - Article I: Architectural Foundation (3 principles)
  - Article II: Code Quality Standards (4 principles)
  - Article III: Testing Philosophy (4 principles)
  - Article IV: Data Layer Governance (3 principles)
  - Article V: API Design Principles (3 principles)
  - Article VI: Security Framework (4 principles)
  - Article VII: Error Handling & Observability (3 principles)
  - Article VIII: Frontend Architecture (3 principles)
  - Article IX: Simplicity & Anti-Complexity Gates (3 principles)
  - Article X: Documentation Standards (3 principles)
  - Article XI: Performance & Scalability (3 principles)
  - Article XII: Development Workflow Principles (4 principles)
  - Article XIII: Governance & Compliance (3 principles)
  - Enforcement Framework section
  - Amendment Log structure

Modified principles: N/A (initial creation)
Removed sections: N/A (initial creation)

Templates requiring updates:
  ✅ plan-template.md - Constitution Check section compatible
  ✅ spec-template.md - User stories/requirements structure compatible
  ✅ tasks-template.md - Phase structure supports TDD workflow

Follow-up TODOs: None
================================================================================
-->

# .NET Fullstack Development Constitution

## Preamble

This Constitution establishes the immutable architectural foundation governing all specifications,
technical plans, task breakdowns, and implementations for this .NET fullstack development project.
These principles ensure quality, maintainability, security, and professional engineering practices
suitable for long-term evolution, team collaboration, and enterprise deployment.

All project artifacts—including specifications, implementation plans, and task definitions—MUST
conform to the principles herein. This Constitution enables efficient multi-agent workflows,
context-optimised execution, and high-quality automated validation through Claude Code.

---

## Article I: Architectural Foundation

### I.1 Clean Architecture Mandate

**Statement**: The solution MUST implement strict separation of concerns through layered
architecture: Domain → Application → Infrastructure → Presentation. Dependencies flow inward;
outer layers depend on inner layers, never the reverse.

**Rationale**: Clean Architecture ensures the core business logic remains independent of
frameworks, databases, and UI concerns. This isolation enables testing without external
dependencies, technology migration without core rewrites, and clear boundaries for team ownership.

**Enforcement Mechanism**:
- Static analysis validates project references flow inward only
- Code review gates reject cross-layer violations
- `dotnet build` with architecture tests in CI pipeline

**Gate Status**:
- PASS: All dependencies flow inward; no Infrastructure references in Domain/Application
- WARNING: Convenience shortcuts exist but are isolated and documented
- CRITICAL: Domain layer references Infrastructure or external frameworks directly

---

### I.2 Domain-Driven Design Alignment

**Statement**: Core business logic MUST reside in the Domain layer with no external dependencies.
Entities, value objects, aggregates, and domain services express business concepts using
ubiquitous language shared with domain experts.

**Rationale**: DDD alignment ensures the codebase mirrors business reality. When code speaks
the same language as stakeholders, requirements translation errors decrease and the system
evolves naturally with business needs.

**Enforcement Mechanism**:
- Domain project has zero package references beyond .NET base libraries
- Entity/value object naming reviewed against domain glossary
- Domain events named using business terminology

**Gate Status**:
- PASS: Domain layer is dependency-free; names match ubiquitous language
- WARNING: Technical naming exists but is documented with business mapping
- CRITICAL: Domain layer depends on ORM, HTTP, or infrastructure packages

---

### I.3 Modular Decomposition

**Statement**: Features MUST be designed as independent, loosely-coupled modules that can be
implemented, tested, and reviewed in isolation. Cross-module communication occurs through
well-defined interfaces or events.

**Rationale**: Modular design enables parallel development workflows, reduces merge conflicts,
allows independent deployment, and limits blast radius of changes. Each module can be assigned
to different agents or developers without coordination overhead.

**Enforcement Mechanism**:
- Feature folders/projects encapsulate all related code
- Module dependencies declared explicitly in project files
- Integration points use interfaces registered in DI container

**Gate Status**:
- PASS: Modules have clear boundaries; changes isolated to single module
- WARNING: Shared utilities exist but follow stable interface contracts
- CRITICAL: Circular dependencies between modules; changes ripple across boundaries

---

## Article II: Code Quality Standards

### II.1 Single Responsibility Enforcement

**Statement**: Every class, method, and module MUST have one reason to change. Classes should
have a single responsibility; methods should do one thing well. Violations require documented
justification with planned remediation.

**Rationale**: Single responsibility reduces coupling, simplifies testing, improves readability,
and makes changes predictable. When a class has multiple responsibilities, changes for one
reason risk breaking unrelated functionality.

**Enforcement Mechanism**:
- Code review checklist includes SRP verification
- Classes exceeding 200 lines trigger review
- Methods exceeding 30 lines require justification

**Gate Status**:
- PASS: Each class/method has clear, singular purpose
- WARNING: Class handles related concerns but could be split
- CRITICAL: God classes; methods with multiple unrelated operations

---

### II.2 Interface Segregation

**Statement**: Clients MUST NOT depend on interfaces they do not use. Prefer small, focused
contracts over large, general-purpose ones. Interface methods should be cohesive and minimal.

**Rationale**: Fat interfaces force implementers to provide functionality they don't need and
consumers to depend on methods they never call. Segregated interfaces enable precise
dependency injection and cleaner testing.

**Enforcement Mechanism**:
- Interfaces reviewed for cohesion during code review
- Interfaces with more than 7 methods require justification
- `NotImplementedException` in interface implementations is a critical violation

**Gate Status**:
- PASS: All interface methods used by all consumers
- WARNING: Interface has optional methods but documents which are required
- CRITICAL: Implementations throw `NotImplementedException`; interfaces exceed 10 methods

---

### II.3 Explicit Over Implicit

**Statement**: Configuration, dependencies, and behavior MUST be explicit. Magic strings, hidden
conventions, auto-wiring by convention, and implicit state are prohibited. All dependencies
injected through constructors; all configuration loaded through typed options.

**Rationale**: Explicit code is discoverable, debuggable, and refactorable. Implicit behavior
hides dependencies, makes testing difficult, and creates tribal knowledge requirements that
impede team scaling.

**Enforcement Mechanism**:
- Constructor injection for all dependencies
- `IOptions<T>` pattern for configuration
- No `Activator.CreateInstance` or reflection-based instantiation outside framework code

**Gate Status**:
- PASS: All dependencies visible in constructor; configuration strongly typed
- WARNING: Convention exists but is documented in CLAUDE.md
- CRITICAL: Service locator pattern; magic strings in core logic; hidden dependencies

---

### II.4 Self-Documenting Code

**Statement**: Code MUST be readable and understandable without extensive comments. Clear
naming, small functions, consistent patterns, and expressive type usage are mandatory.
Comments explain "why" not "what."

**Rationale**: Code is read far more often than written. Self-documenting code reduces
onboarding time, prevents comment drift, and enables faster code review. When code needs
comments to explain what it does, refactoring is required.

**Enforcement Mechanism**:
- Naming conventions enforced by analyzers (PascalCase public, _camelCase private)
- Methods named with verb phrases; classes named with noun phrases
- Comments that restate code flagged in review

**Gate Status**:
- PASS: Code intent clear from reading; comments add context not available in code
- WARNING: Complex algorithm has explanatory comment
- CRITICAL: Comments explain what code does; misleading names; abbreviations without context

---

## Article III: Testing Philosophy

### III.1 Test-First Imperative

**Statement**: No production code may be written until corresponding tests exist and fail
(Red-Green-Refactor). This is NON-NEGOTIABLE. Tests define the expected behavior before
implementation begins.

**Rationale**: TDD ensures testability is designed in, not bolted on. Tests serve as executable
specifications, catch regressions immediately, and provide safe refactoring. Writing tests
first forces clear thinking about requirements and interfaces.

**Enforcement Mechanism**:
- Task workflow requires test task completion before implementation task
- CI fails if new code lacks corresponding test coverage
- `dotnet test` must pass before any commit

**Gate Status**:
- PASS: Tests written first; all tests pass; coverage maintained
- WARNING: Spike code exists but is marked TODO with test task created
- CRITICAL: Production code merged without tests; tests written after implementation

---

### III.2 Testing Pyramid Compliance

**Statement**: Unit tests form the base (fast, isolated, numerous), integration tests validate
component interaction (moderate quantity), and end-to-end tests verify critical user journeys
(few, high-value). The pyramid shape MUST be maintained.

**Rationale**: Unit tests provide fast feedback and pinpoint failures. Integration tests verify
contracts between components. E2E tests validate business value but are slow and brittle.
Inverting the pyramid creates slow, unreliable test suites.

**Enforcement Mechanism**:
- Test counts reported in CI: unit > integration > E2E by order of magnitude
- Integration tests use test containers or in-memory databases
- E2E tests limited to critical paths documented in spec

**Gate Status**:
- PASS: Unit tests outnumber integration tests 10:1; E2E covers critical paths only
- WARNING: Ratio approaching inversion but documented remediation plan exists
- CRITICAL: More E2E tests than unit tests; integration tests hit production services

---

### III.3 Meaningful Test Coverage

**Statement**: Target 80%+ code coverage with emphasis on business logic, edge cases, and error
paths. Coverage without assertion quality is meaningless. Each test MUST assert specific,
meaningful outcomes.

**Rationale**: Coverage metrics ensure code paths are exercised but don't guarantee correctness.
Quality assertions verify behavior. Focus coverage on Domain and Application layers where
business logic lives.

**Enforcement Mechanism**:
- Coverage thresholds enforced in CI (80% line coverage minimum)
- Mutation testing identifies weak assertions
- Tests require at least one assert; arrange-act-assert structure enforced

**Gate Status**:
- PASS: 80%+ coverage; meaningful assertions; mutation score acceptable
- WARNING: Coverage 70-80% with documented plan to improve
- CRITICAL: Coverage below 70%; tests without assertions; tests that never fail

---

### III.4 Automated Validation Gates

**Statement**: All tests MUST be runnable via CLI commands (`dotnet test`) to enable automated
verification through hooks and CI/CD pipelines. Interactive tests, manual verification steps,
and tests requiring human judgment are prohibited in the automated suite.

**Rationale**: Automation enables continuous integration, pre-commit hooks, and parallel
execution. Tests that require human intervention cannot gate deployments or provide
consistent feedback.

**Enforcement Mechanism**:
- `dotnet test` executes full test suite
- CI pipeline runs tests on every push
- Pre-commit hooks run fast unit tests

**Gate Status**:
- PASS: All tests automated; CI green; hooks execute tests
- WARNING: Manual test documented for edge case outside automation capability
- CRITICAL: Tests require manual setup; tests flaky without explanation

---

## Article IV: Data Layer Governance

### IV.1 Repository Pattern Mandate

**Statement**: All data access MUST occur through repository abstractions defined in the
Application layer. Infrastructure layer provides implementations. DbContext is never injected
directly into Application or Domain layers.

**Rationale**: Repository abstraction enables unit testing without databases, allows storage
technology changes without business logic rewrites, and enforces query patterns. Direct
DbContext usage couples business logic to EF Core.

**Enforcement Mechanism**:
- Repository interfaces in Application layer
- Repository implementations in Infrastructure layer
- Code review rejects DbContext injection outside Infrastructure

**Gate Status**:
- PASS: All data access through repository interfaces
- WARNING: Direct DbContext in Infrastructure service with documented reason
- CRITICAL: DbContext injected into Application layer; queries in controllers

---

### IV.2 Migration-First Schema Evolution

**Statement**: Database schema changes MUST occur exclusively through versioned EF Core
migrations. Manual schema modifications, direct database edits, and schema-modifying scripts
outside migration framework are prohibited.

**Rationale**: Migrations provide version control for schema, enable rollback, ensure
reproducibility across environments, and document schema evolution history. Manual changes
create environment drift.

**Enforcement Mechanism**:
- `dotnet ef migrations add` for all schema changes
- CI applies migrations to test database
- Production deployment runs `dotnet ef database update`

**Gate Status**:
- PASS: All schema changes in migrations; migrations tested in CI
- WARNING: Data migration script exists but is linked to EF migration
- CRITICAL: Manual schema change; migration not tested; schema drift detected

---

### IV.3 Query Optimization Standards

**Statement**: Complex queries MUST include execution plan analysis. N+1 query patterns are
CRITICAL violations. All queries use `.AsNoTracking()` for read-only operations. Eager loading
specified explicitly with `.Include()`.

**Rationale**: Database queries are common performance bottlenecks. N+1 patterns multiply
database round trips. Tracking overhead wastes memory for read-only scenarios. Implicit lazy
loading hides performance problems.

**Enforcement Mechanism**:
- EF Core logging enabled in development to surface queries
- Code review checks for `.Include()` presence
- Performance tests verify query counts

**Gate Status**:
- PASS: Queries optimized; explicit loading; no N+1 patterns
- WARNING: Query count higher than expected but within acceptable bounds
- CRITICAL: N+1 pattern detected; queries in loops; missing `.AsNoTracking()`

---

## Article V: API Design Principles

### V.1 Contract-First Development

**Statement**: API contracts (OpenAPI/Swagger specifications) MUST be defined and agreed upon
before implementation begins. Contract changes require versioning consideration. Generated
clients should compile against contract.

**Rationale**: Contract-first enables parallel frontend/backend development, documents API
behavior, enables automated client generation, and forces intentional design rather than
implementation leakage.

**Enforcement Mechanism**:
- OpenAPI spec reviewed before endpoint implementation
- Contract tests verify implementation matches spec
- Breaking changes require new API version

**Gate Status**:
- PASS: Contract defined first; implementation matches spec; tests verify
- WARNING: Minor deviation documented with spec update planned
- CRITICAL: Implementation differs from contract; no contract exists; breaking change without version

---

### V.2 RESTful Resource Design

**Statement**: APIs MUST expose resources, not operations. HTTP verbs convey intent (GET=read,
POST=create, PUT=replace, PATCH=update, DELETE=remove). Response codes communicate outcomes
accurately (2xx success, 4xx client error, 5xx server error).

**Rationale**: RESTful design provides predictable, cacheable, scalable APIs. Verb-based
operations (GET /users/create) violate HTTP semantics, break caching, and confuse clients.
Accurate status codes enable proper error handling.

**Enforcement Mechanism**:
- Route naming reviewed for noun-based resources
- HTTP method usage validated in code review
- Response codes mapped to Problem Details (RFC 7807)

**Gate Status**:
- PASS: Resources are nouns; verbs match HTTP semantics; codes accurate
- WARNING: RPC-style endpoint exists for complex operation with documentation
- CRITICAL: Verbs in URLs; GET with side effects; 200 returned for errors

---

### V.3 Versioning Strategy

**Statement**: API versioning MUST be implemented from inception using URL path versioning
(`/api/v1/`). Breaking changes require new versions. Deprecated versions have documented
sunset timelines. Multiple versions may run concurrently.

**Rationale**: Versioning enables API evolution without breaking existing clients. URL-based
versioning is explicit, cacheable, and debuggable. Sunset timelines give clients migration
runway.

**Enforcement Mechanism**:
- All routes include version prefix
- Breaking change checklist in PR template
- Deprecated endpoints logged with sunset date

**Gate Status**:
- PASS: All endpoints versioned; breaking changes in new version; sunset documented
- WARNING: Internal-only API without versioning (documented exception)
- CRITICAL: Breaking change in existing version; no versioning; silent deprecation

---

## Article VI: Security Framework

### VI.1 Defence in Depth

**Statement**: Security controls MUST exist at every layer—network, application, data. No
single point of failure. Authentication at edge, authorization at service, encryption at rest
and in transit.

**Rationale**: Layered security ensures that breach of one layer doesn't compromise the system.
Attackers must defeat multiple controls. Defence in depth is a foundational security principle.

**Enforcement Mechanism**:
- HTTPS required for all endpoints
- Authorization attributes on all controllers/actions
- Database encryption enabled; connection strings secured

**Gate Status**:
- PASS: Security controls at all layers; no single points of failure
- WARNING: Development environment has relaxed controls (documented)
- CRITICAL: Single authentication point; unencrypted sensitive data; open endpoints

---

### VI.2 Authentication & Authorisation Separation

**Statement**: Identity verification (authentication) and permission validation (authorisation)
MUST be distinct concerns with separate implementations. Authentication confirms identity;
authorization confirms access rights.

**Rationale**: Separation enables flexible security policies. Identity providers can change
without affecting authorization. Role-based, claim-based, and policy-based authorization can
coexist.

**Enforcement Mechanism**:
- Authentication middleware separate from authorization policies
- `[Authorize]` attributes specify policies, not authentication schemes
- Claims used for authorization decisions

**Gate Status**:
- PASS: Clear separation; authentication in middleware; authorization via policies
- WARNING: Simple application uses combined approach (documented)
- CRITICAL: Authorization decisions based on authentication method; no policy abstraction

---

### VI.3 Secrets Management

**Statement**: No secrets in source code. All sensitive configuration (connection strings, API
keys, certificates) MUST use secure vaults (Azure Key Vault, AWS Secrets Manager) or
environment-based injection. Secrets never logged.

**Rationale**: Secrets in code are exposed in version control, build logs, and error messages.
Secret management systems provide rotation, auditing, and access control.

**Enforcement Mechanism**:
- Pre-commit hook scans for secret patterns
- Configuration uses `IOptions<T>` with environment variable binding
- Key Vault integration for production secrets

**Gate Status**:
- PASS: No secrets in code; vault integration; rotation enabled
- WARNING: Development uses user secrets (not in repo)
- CRITICAL: Secret in source code; secret in logs; hardcoded credentials

---

### VI.4 Input Validation

**Statement**: All external input MUST be treated as untrusted. Validation occurs at system
boundaries with fail-safe defaults. Use FluentValidation or Data Annotations. Reject invalid
input early; never sanitize and proceed.

**Rationale**: Input validation prevents injection attacks, data corruption, and unexpected
behavior. Fail-safe defaults ensure that missing validation doesn't create vulnerabilities.
Early rejection provides clear feedback.

**Enforcement Mechanism**:
- Validation attributes on all DTOs
- FluentValidation for complex rules
- API returns 400 with Problem Details for validation failures

**Gate Status**:
- PASS: All inputs validated; validation at boundaries; clear error messages
- WARNING: Validation deferred for performance with documented risk acceptance
- CRITICAL: Unvalidated input reaches database; SQL/command injection possible

---

## Article VII: Error Handling & Observability

### VII.1 Structured Exception Handling

**Statement**: Custom exception hierarchies MUST distinguish between recoverable and fatal
errors. Generic catch blocks (`catch (Exception)`) are prohibited except at application
boundaries. Exceptions include context for debugging.

**Rationale**: Typed exceptions enable specific handling strategies. Generic catch blocks hide
bugs and make debugging difficult. Context in exceptions reduces mean time to resolution.

**Enforcement Mechanism**:
- Custom exceptions inherit from domain-specific base
- Catch blocks specify exception type
- Global exception handler at API boundary only

**Gate Status**:
- PASS: Typed exceptions; specific catch blocks; contextual messages
- WARNING: Generic catch with logging and rethrow
- CRITICAL: Exception swallowed silently; catch (Exception) without rethrow

---

### VII.2 Comprehensive Logging

**Statement**: All significant operations MUST produce structured log entries with correlation
IDs enabling request flow reconstruction. Use log levels appropriately (Error, Warning, Info,
Debug). PII never logged.

**Rationale**: Logs are the primary diagnostic tool in production. Structured logging enables
querying. Correlation IDs connect distributed operations. Appropriate levels prevent log noise.

**Enforcement Mechanism**:
- Serilog with structured logging configured
- Correlation ID middleware adds ID to all requests
- Log level guidelines documented

**Gate Status**:
- PASS: Structured logs; correlation IDs; appropriate levels; no PII
- WARNING: Log verbosity high but filterable
- CRITICAL: Unstructured logs; no correlation; PII in logs; missing error logs

---

### VII.3 Health & Diagnostics

**Statement**: Applications MUST expose health check endpoints (`/health`, `/health/ready`,
`/health/live`) and diagnostic telemetry for operational monitoring. Health checks verify
dependencies.

**Rationale**: Health endpoints enable load balancer routing, container orchestration, and
alerting. Dependency checks surface issues before users encounter them.

**Enforcement Mechanism**:
- `Microsoft.Extensions.Diagnostics.HealthChecks` configured
- Database, cache, and external service checks included
- Kubernetes probes point to health endpoints

**Gate Status**:
- PASS: Health endpoints exist; dependencies checked; telemetry configured
- WARNING: Basic health check without dependency verification
- CRITICAL: No health endpoints; no monitoring; silent failures

---

## Article VIII: Frontend Architecture

### VIII.1 Component-Based Design

**Statement**: UI MUST be composed of reusable, testable components with clear props/state
boundaries. Components are single-responsibility. Presentational and container components
separated.

**Rationale**: Component architecture enables reuse, isolation, and testability. Clear
boundaries prevent prop drilling and state confusion. Separation of concerns makes components
predictable.

**Enforcement Mechanism**:
- Component files limited to single component export
- Props typed with TypeScript interfaces
- Component tests verify rendering and interaction

**Gate Status**:
- PASS: Small, focused components; typed props; testable
- WARNING: Large component with documented splitting plan
- CRITICAL: God components; untyped props; untestable UI

---

### VIII.2 State Management Discipline

**Statement**: Application state MUST follow predictable patterns. Mutations are traceable and
auditable. Server state and client state separated. Caching strategy explicit.

**Rationale**: Predictable state management prevents bugs from unexpected mutations. Separation
of server/client state enables proper caching and invalidation. Auditability aids debugging.

**Enforcement Mechanism**:
- State management library used consistently (Redux, Zustand, etc.)
- Server state managed with data fetching library (React Query, SWR)
- State updates logged in development

**Gate Status**:
- PASS: Predictable state; traceable mutations; proper caching
- WARNING: Mixed patterns with migration plan documented
- CRITICAL: Scattered state; mutations not traceable; cache invalidation missing

---

### VIII.3 Accessibility Compliance

**Statement**: WCAG 2.1 AA compliance is MANDATORY. Accessibility is a feature, not an
afterthought. Semantic HTML, ARIA attributes where needed, keyboard navigation, and color
contrast requirements apply.

**Rationale**: Accessibility ensures the application is usable by everyone. Legal requirements
in many jurisdictions mandate accessibility. Accessible design often improves usability for all.

**Enforcement Mechanism**:
- Accessibility linting in CI (eslint-plugin-jsx-a11y)
- Manual accessibility audit before release
- Keyboard navigation tested

**Gate Status**:
- PASS: WCAG 2.1 AA compliant; semantic HTML; keyboard navigable
- WARNING: Minor issues documented with remediation timeline
- CRITICAL: Inaccessible UI; missing alt text; no keyboard support

---

## Article IX: Simplicity & Anti-Complexity Gates

### IX.1 YAGNI Enforcement

**Statement**: Speculative features and premature abstractions are PROHIBITED. Every abstraction
MUST solve a current, documented problem. "We might need this later" is not justification.

**Rationale**: Unused abstractions add maintenance burden, cognitive load, and code surface
area. Building for hypothetical future requirements often builds the wrong thing. Simple code
is easier to change when requirements become real.

**Enforcement Mechanism**:
- Code review challenges abstractions without current use
- Pattern introduction requires documented problem statement
- Unused code detected and removed

**Gate Status**:
- PASS: All code serves current requirements; abstractions have multiple uses
- WARNING: Abstraction exists for one use but documented need for second imminent
- CRITICAL: Unused abstractions; speculative generalization; premature patterns

---

### IX.2 Maximum Project Limit

**Statement**: Solutions MUST contain the minimum number of projects necessary. Additional
projects require explicit justification documented in the implementation plan. Default to
fewer, larger projects.

**Rationale**: Each project adds build time, dependency management, and cognitive overhead.
Many small projects often indicate premature modularization. Consolidation is easier than
distribution.

**Enforcement Mechanism**:
- New project creation requires justification in PR description
- Project count reviewed quarterly
- Merge candidates identified for underutilized projects

**Gate Status**:
- PASS: Minimum viable projects; each project has clear purpose
- WARNING: Extra project exists with documented reason
- CRITICAL: Projects with single class; circular project references; build time excessive

---

### IX.3 Dependency Scrutiny

**Statement**: Every external dependency MUST be justified. Prefer framework-native capabilities
over third-party alternatives when functionally equivalent. Evaluate security, maintenance,
and license for all dependencies.

**Rationale**: Dependencies introduce upgrade burden, security surface area, and potential
abandonment risk. Framework-native solutions receive maintenance with the framework.
Unnecessary dependencies complicate auditing.

**Enforcement Mechanism**:
- New dependency requires justification in PR
- `dotnet outdated` run weekly
- License and security audit for new packages

**Gate Status**:
- PASS: Dependencies justified; framework-native preferred; no vulnerable packages
- WARNING: Dependency duplicates framework capability with documented reason
- CRITICAL: Abandoned dependency; vulnerable package; license incompatibility

---

## Article X: Documentation Standards

### X.1 Living Documentation

**Statement**: Code is the primary documentation. XML documentation comments MUST exist on all
public APIs. README files at solution and project levels. Documentation updated with code.

**Rationale**: External documentation drifts from code. XML comments appear in IntelliSense,
staying visible during development. READMEs provide entry points. Living documentation
maintains accuracy.

**Enforcement Mechanism**:
- Compiler warning for missing XML documentation on public members
- README.md required in each project
- Documentation review in PR checklist

**Gate Status**:
- PASS: Public APIs documented; READMEs current; no doc drift
- WARNING: Internal class missing docs but external API complete
- CRITICAL: Public API undocumented; README outdated; misleading documentation

---

### X.2 Architecture Decision Records

**Statement**: Significant architectural decisions MUST be documented with context, decision,
and consequences in ADR format. ADRs are immutable once accepted; superseded ADRs reference
their replacement.

**Rationale**: ADRs capture the "why" that code cannot express. Future maintainers understand
constraints and alternatives considered. Immutability preserves historical context.

**Enforcement Mechanism**:
- ADR template in docs/decisions/
- Architectural PR requires ADR reference
- ADR review in architecture meetings

**Gate Status**:
- PASS: Decisions documented; context clear; alternatives listed
- WARNING: ADR exists but consequences section incomplete
- CRITICAL: Major decision undocumented; ADR modified after acceptance

---

### X.3 Runbook Requirements

**Statement**: Operational procedures for deployment, monitoring, and incident response MUST
exist before production release. Runbooks include troubleshooting guides and escalation paths.

**Rationale**: Operations cannot wait for documentation during incidents. Runbooks enable
on-call rotation and reduce mean time to recovery. Pre-production runbooks ensure operational
readiness.

**Enforcement Mechanism**:
- Production release checklist includes runbook verification
- Runbook tested during staging deployment
- On-call rotation requires runbook familiarity

**Gate Status**:
- PASS: Runbooks complete; tested; accessible to operations team
- WARNING: Runbook exists but untested
- CRITICAL: No runbook; outdated procedures; inaccessible during incident

---

## Article XI: Performance & Scalability

### XI.1 Baseline Metrics

**Statement**: Acceptable response times, throughput, and resource utilisation targets MUST be
defined before implementation. Performance tests verify baselines. Deviations investigated.

**Rationale**: Without baselines, performance cannot be measured or regressed. Upfront targets
guide design decisions. Performance is a feature that requires intentional implementation.

**Enforcement Mechanism**:
- Performance requirements in feature spec
- Load tests in CI pipeline
- Performance budget alerts

**Gate Status**:
- PASS: Baselines defined; tests verify; within budget
- WARNING: Baseline exceeded with documented optimization plan
- CRITICAL: No baselines defined; performance untested; significant regression

---

### XI.2 Async-First

**Statement**: I/O-bound operations MUST use asynchronous patterns (`async/await`). Blocking
calls (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) require documented justification and
are prohibited in request paths.

**Rationale**: Async operations free threads during I/O, enabling scalability. Blocking on
async code causes deadlocks and thread pool starvation. .NET is designed for async-first I/O.

**Enforcement Mechanism**:
- Analyzer rules flag blocking calls
- Async suffix on async methods
- Code review rejects blocking in controllers/services

**Gate Status**:
- PASS: All I/O async; no blocking calls; async throughout stack
- WARNING: Blocking in startup/initialization with documented reason
- CRITICAL: `.Result` in request path; deadlock potential; sync over async

---

### XI.3 Caching Strategy

**Statement**: Caching decisions MUST be deliberate, documented, and include invalidation
strategies. Cache-aside pattern preferred. TTLs explicit. Distributed caching for
multi-instance deployments.

**Rationale**: Effective caching dramatically improves performance. Poor caching causes stale
data bugs. Invalidation is the hardest problem; explicit strategies prevent issues.
Distributed caching avoids inconsistency.

**Enforcement Mechanism**:
- Caching documented in design
- `IMemoryCache` for single instance; `IDistributedCache` for multi-instance
- Cache hit/miss metrics collected

**Gate Status**:
- PASS: Caching intentional; invalidation clear; metrics tracked
- WARNING: Caching exists without invalidation for static data
- CRITICAL: Stale data from caching; no invalidation strategy; memory leaks

---

## Article XII: Development Workflow Principles

### XII.1 Task Independence

**Statement**: Implementation tasks SHOULD be structured to minimise dependencies where possible,
enabling parallel execution. Each task should be completable by a single agent without waiting
for other task completion.

**Rationale**: Independent tasks enable parallel workflows, reduce bottlenecks, and allow
flexible prioritization. Dependencies create coordination overhead and idle time.

**Enforcement Mechanism**:
- Task definition includes dependency analysis
- Circular task dependencies rejected
- Parallel markers ([P]) in task lists

**Gate Status**:
- PASS: Tasks maximally independent; parallel opportunities identified
- WARNING: Sequential dependency exists but documented as necessary
- CRITICAL: Artificial dependencies; circular dependencies; parallelism blocked

---

### XII.2 Incremental Validation

**Statement**: Each task MUST produce a testable, verifiable outcome. Work should be validated
continuously through automated tests, not only at completion. Small commits with passing tests.

**Rationale**: Continuous validation catches issues early when they're cheap to fix. Large
batches hide problems and make debugging difficult. Green test suites maintain confidence.

**Enforcement Mechanism**:
- Each task includes verification step
- Pre-commit hooks run tests
- CI runs on every push

**Gate Status**:
- PASS: Continuous green builds; small commits; verified outcomes
- WARNING: Task spans multiple commits but tests pass at end
- CRITICAL: Long periods without validation; broken builds ignored

---

### XII.3 Context Efficiency

**Statement**: Large operations SHOULD be decomposed into focused sub-tasks to maintain quality
and enable specialist handling. Each sub-task should have clear boundaries and deliverables.

**Rationale**: Focused tasks enable better agent assignment, clearer progress tracking, and
higher quality output. Context switching between unrelated concerns degrades quality.

**Enforcement Mechanism**:
- Tasks estimated; large tasks split
- Sub-tasks have clear acceptance criteria
- Specialist agents assigned to matching tasks

**Gate Status**:
- PASS: Tasks appropriately sized; clear boundaries; specialist assignment
- WARNING: Large task exists but has internal checkpoints
- CRITICAL: Monolithic tasks; unclear boundaries; context overload

---

### XII.4 Deterministic Automation

**Statement**: Quality gates (linting, formatting, testing) MUST be automatable via CLI commands
with deterministic outcomes. Same input always produces same pass/fail result. Flaky automation
is a critical defect.

**Rationale**: Automation enables CI/CD, pre-commit hooks, and consistent quality enforcement.
Non-deterministic results undermine trust in automation and lead to ignored failures.

**Enforcement Mechanism**:
- `dotnet format` for formatting
- `dotnet test` for testing
- Flaky test detection and quarantine

**Gate Status**:
- PASS: All gates automated; deterministic results; no flakiness
- WARNING: Known flaky test quarantined with fix in progress
- CRITICAL: Manual quality gates; flaky tests in main suite; inconsistent results

---

## Article XIII: Governance & Compliance

### XIII.1 Constitution Supremacy

**Statement**: This Constitution supersedes all other practices. Conflicts between this
Constitution and other guidelines, conventions, or preferences are resolved in favour of
constitutional principles. No exception.

**Rationale**: A constitution without supremacy is merely a suggestion. Consistent application
of principles across all work ensures quality and predictability. Exceptions erode governance.

**Enforcement Mechanism**:
- PR reviews verify constitutional compliance
- Violations block merge
- Appeals process through Amendment Protocol

**Gate Status**:
- PASS: All work complies with Constitution
- WARNING: N/A (compliance is binary)
- CRITICAL: Constitutional violation merged; governance ignored

---

### XIII.2 Amendment Protocol

**Statement**: Constitutional changes require: (1) documented justification with problem
statement, (2) impact analysis on existing artifacts, (3) explicit approval from project
governance, and (4) version increment per semantic versioning.

**Rationale**: Governance stability enables planning and consistency. Amendment friction ensures
changes are deliberate and well-considered. Impact analysis prevents unintended consequences.

**Enforcement Mechanism**:
- Amendment PR includes justification and impact
- Review by designated approvers
- Version updated in Constitution

**Gate Status**:
- PASS: Amendment follows protocol; version updated; artifacts synced
- WARNING: Minor clarification without version bump
- CRITICAL: Constitution modified without protocol; no version tracking

---

### XIII.3 Deviation Documentation

**Statement**: Any permitted deviation from constitutional principles MUST be documented with:
(1) specific principle being deviated from, (2) rationale for deviation, (3) risk acceptance,
and (4) planned remediation with timeline.

**Rationale**: Documented deviations enable tracking and eventual remediation. Undocumented
deviations become permanent technical debt. Explicit risk acceptance ensures accountability.

**Enforcement Mechanism**:
- Deviation logged in PR description
- Deviation tracker maintained
- Quarterly deviation review

**Gate Status**:
- PASS: No deviations or all deviations documented with remediation
- WARNING: Deviation documented without timeline
- CRITICAL: Undocumented deviation; permanent deviation without acceptance

---

## Enforcement Framework

### Gate Classification

| Status | Definition | Action Required |
|--------|------------|-----------------|
| PASS | Full compliance with principle | Proceed |
| WARNING | Minor deviation with justification | Document and proceed |
| CRITICAL | Principle violation | Block until resolved |

### Automated Enforcement

The following quality gates MUST be automated and run on every change:

```bash
# Build verification
dotnet build --warnaserror

# Test execution
dotnet test --collect:"XPlat Code Coverage"

# Code formatting
dotnet format --verify-no-changes

# Security scanning
dotnet list package --vulnerable
```

### Review Checklist

Every pull request MUST verify:

- [ ] Clean Architecture: dependencies flow inward
- [ ] TDD: tests written before implementation
- [ ] No secrets in code
- [ ] Public APIs documented
- [ ] No CRITICAL gate violations
- [ ] All WARNING deviations documented

---

## Amendment Log

| Version | Date | Summary | Author |
|---------|------|---------|--------|
| 1.0.0 | 2026-01-23 | Initial ratification - 13 articles, 40 principles | Constitution |

---

**Version**: 1.0.0 | **Ratified**: 2026-01-23 | **Last Amended**: 2026-01-23
