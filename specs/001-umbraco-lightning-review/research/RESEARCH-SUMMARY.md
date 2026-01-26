# CoinGecko API Research - Executive Summary

**Date**: 2026-01-26
**Status**: Complete
**Target Specifications**: FR-019 through FR-022 (Multi-Currency Display)

## Quick Reference

| Aspect | Recommendation |
|--------|-----------------|
| **API Endpoint** | `GET /api/v3/simple/price` |
| **Free Tier Limit** | 10-50 requests/minute |
| **Pro Tier Limit** | 50 requests/minute guaranteed |
| **Cache TTL** | 5 minutes |
| **Request Timeout** | 10 seconds |
| **Retry Strategy** | 3 attempts with exponential backoff (50ms, 100ms, 200ms) |
| **Supported Currencies** | 150+ fiat currencies via `/simple/supported_vs_currencies` |
| **Response Format** | Clean JSON, no wrapper object |
| **Authentication** | Optional Pro API key in `x-cg-pro-api-key` header |

---

## Implementation Roadmap

### Phase 1: Core Integration (Minimal Viable Product)
**Effort**: 2-3 days

1. Create domain value objects (`ExchangeRate`, `ExchangeRates`)
2. Implement `CoinGeckoHttpClient` with error handling
3. Register `CoinGeckoExchangeRateService` in DI
4. Add simple in-memory caching
5. Expose exchange rate endpoint in admin API

**Outcome**: Basic fiat conversion working, 5-minute cache, free tier support

### Phase 2: Production Hardening (Week 2)
**Effort**: 2-3 days

1. Implement distributed caching (Redis/SQL Server)
2. Add comprehensive resilience policies (Polly)
3. Implement rate limit monitoring
4. Add OpenTelemetry metrics
5. Create admin dashboard for cache management

**Outcome**: Production-ready, monitored, horizontally scalable

### Phase 3: Pro Tier Support (Optional, Week 3+)
**Effort**: 1 day

1. Add Pro API key configuration
2. Create upgrade guidance based on metrics
3. Implement conditional header injection

**Outcome**: Upgrade path documented, easy configuration

---

## Key Decisions

### 1. Use Free Tier by Default
**Rationale**:
- 10-50 requests/minute sufficient for most sites
- With 5-minute caching: ~288 daily API calls vs 1,440 potential requests
- Zero cost, simple deployment
- Upgrade path easy when needed

**When to Upgrade**:
- >10K daily unique visitors
- Real-time rate requirements (<5 min freshness)
- SLA requirements (99.9% uptime)

### 2. Implement Distributed Cache
**Rationale**:
- Multi-server deployments need cache coherency
- Redis/SQL Server options both viable
- Prevents thundering herd on cache miss
- Supports future scaling

**Fallback**: In-memory cache for single-server deployments

### 3. Cache TTL: 5 Minutes
**Rationale**:
- Bitcoin price changes continuously but not millisecond-level important
- User refreshes page at least 5 minutes apart
- Balances freshness vs API call reduction
- Aligns with specification requirement (FR-021)

### 4. Graceful Degradation Strategy
**When rates unavailable**:
1. Return cached rate (if available, even if stale)
2. Display satoshi amount only (no fiat conversion)
3. Show "rates unavailable" message to user
4. Log error for monitoring
5. Never fail the payment flow

---

## Critical Implementation Points

### Error Handling Hierarchy

```
ExchangeRateException (custom domain exception)
├─ Network Errors (transient) → Retry with backoff
├─ Rate Limit (429) → Fallback to cache
├─ Server Error (5xx) → Fallback to stale cache
├─ Invalid Request (400) → Log and return cached value
└─ Permanent Failures → Display satoshi-only
```

### Caching Strategy

```
Request → Check In-Memory → Hit? Return
              ↓ Miss
          Check Distributed Cache
              ↓ Hit
          Return + Populate In-Memory
              ↓ Miss
          Fetch from API
              ↓ Success
          Store in Both Caches
              ↓ Failure
          Try Stale Cache
              ↓ Still Fail
          Return satoshi-only display
```

### Rate Limit Monitoring

Monitor these headers from every response:
- `x-ratelimit-limit-minute`: Max requests per minute
- `x-ratelimit-remaining-minute`: Requests remaining

**Alert Conditions**:
- Remaining < 10% of limit: Log warning
- Remaining < 5% of limit: Alert operations team
- Multiple 429 responses: Suggest Pro upgrade

---

## API Response Patterns

### Success Response
```json
{
  "bitcoin": {
    "usd": 42500.50,
    "eur": 39200.25,
    "gbp": 33800.75,
    "last_updated_at": 1706275200
  }
}
```

### Error Response (429 Rate Limit)
```json
{
  "status": {
    "error_code": 1002,
    "error_message": "You've reached the Rate limit..."
  }
}
```

**Key Point**: Error responses use `status` wrapper, success uses direct cryptocurrency data

---

## Testing Strategy

### Unit Tests
- Mock `ICoinGeckoHttpClient`
- Test cache hit/miss paths
- Test error handling and fallbacks
- Use test builders for fixtures

### Integration Tests
- Real HTTP calls to CoinGecko (optional, marked as integration)
- Verify response parsing
- Test rate limit header extraction
- Mock only network layer, not JSON parsing

### Load Tests
- Simulate 100+ concurrent requests
- Verify cache prevents cascading API calls
- Measure response times with/without cache

---

## Monitoring & Observability

### Key Metrics to Track

```
coingecko.api.calls.total      # Total API calls
coingecko.api.duration.ms      # API call latency
coingecko.cache.hits           # Cache hit count
coingecko.cache.misses         # Cache miss count
coingecko.errors.total         # Error count by type
coingecko.rate.limit.remaining # Remaining requests (from header)
```

### Alerting Rules

- **Warning**: Cache miss rate > 20%
- **Warning**: API response time > 2 seconds
- **Critical**: Rate limit remaining < 5/minute
- **Critical**: Circuit breaker open (persistent failures)

---

## Production Checklist

Before deploying to production:

- [ ] **Configuration**
  - [ ] BaseUrl set correctly (production CoinGecko)
  - [ ] Timeout set to 10 seconds
  - [ ] Cache TTL set to 5 minutes
  - [ ] Pro API key configured (if applicable)

- [ ] **Resilience**
  - [ ] Polly retry policy configured
  - [ ] Circuit breaker enabled
  - [ ] Timeout handling tested
  - [ ] Rate limit responses handled

- [ ] **Caching**
  - [ ] Distributed cache configured
  - [ ] Cache invalidation works
  - [ ] Stale cache fallback tested
  - [ ] Cache TTL appropriate

- [ ] **Error Handling**
  - [ ] All exception paths tested
  - [ ] Graceful degradation verified
  - [ ] User sees clear messages
  - [ ] Logging captures all errors

- [ ] **Monitoring**
  - [ ] Metrics collection enabled
  - [ ] Alert thresholds set
  - [ ] Dashboard created
  - [ ] Rate limit monitoring active

- [ ] **Testing**
  - [ ] Unit tests: 80%+ coverage
  - [ ] Integration tests passing
  - [ ] Load test completed (100+ concurrent)
  - [ ] All test scenarios documented

---

## Deployment Flow

### Development Environment
1. Use free tier API
2. Enable debug logging
3. Use in-memory cache
4. Disable circuit breaker (for testing failures)

### Staging Environment
1. Use free tier API
2. Configure distributed cache (Redis or SQL)
3. Enable circuit breaker with longer break duration
4. Monitor metrics before production

### Production Environment
1. **Option A (Recommended for most)**: Free tier with 5-minute cache
2. **Option B (High-traffic sites)**: Pro tier API key + aggressive monitoring
3. Both: Distributed cache, comprehensive logging, rate limit monitoring

---

## Cost Analysis

### Free Tier
- **API Cost**: $0
- **Infrastructure**: Standard .NET Core + Redis/SQL cache
- **Operational**: Monitor rate limits, manual upgrades when needed
- **For**: Most sites <10K daily visitors

### Pro Tier
- **API Cost**: ~$50-200/month (check CoinGecko pricing)
- **Infrastructure**: Same as free tier
- **Operational**: Guaranteed uptime, SLA support
- **For**: High-traffic sites (>50K daily visitors), mission-critical systems

**Break-even**: Pro tier becomes cost-effective above ~50K daily visitors (price per request drops)

---

## Documentation Provided

This research includes three comprehensive documents:

### 1. **coingecko-api-research.md** (50KB)
- Complete API endpoint documentation
- Free vs Pro tier comparison
- Response format specifications
- Caching patterns and strategies
- Error handling approaches
- Pro API upgrade path

### 2. **coingecko-dotnet-implementation.md** (60KB)
- Production-ready .NET code
- Domain models and value objects
- Service interfaces and implementations
- HTTP client with resilience
- Dependency injection setup
- Configuration patterns
- Complete usage examples

### 3. **coingecko-api-reference.md** (40KB)
- API response examples
- HTTP status codes and error responses
- Rate limiting details
- Test utilities and mock builders
- cURL command reference
- Troubleshooting guide
- Currency code reference

---

## Next Steps

1. **Review Documentation**: Examine the three research documents
2. **Design Domain Models**: Create value objects for exchange rates
3. **Implement HTTP Client**: Create `CoinGeckoHttpClient` with error handling
4. **Implement Service**: Add caching and resilience logic
5. **Configure DI**: Register services and options in composition root
6. **Write Tests**: Unit and integration tests for all paths
7. **Performance Test**: Load test with 100+ concurrent requests
8. **Deploy**: Follow production checklist before launch

---

## Summary

**CoinGecko API is production-ready for Umbraco Lightning Payments multi-currency feature.**

- **Simple API**: One endpoint (`/simple/price`) solves the entire use case
- **Free Tier**: Sufficient for 99% of deployments with intelligent caching
- **Reliable**: Proven service with years of uptime
- **Scalable**: Pro tier upgrade path for future growth
- **Well-Documented**: Clear error responses, rate limit headers, standard REST patterns

**Recommended Approach**:
1. Start with free tier + 5-minute cache
2. Monitor rate limits monthly
3. Upgrade to Pro tier when needed
4. Never let cache failure break payment flow (graceful degradation)

---

## Contact & Support

For issues or questions:
- **CoinGecko Status**: https://status.coingecko.com/
- **CoinGecko Docs**: https://www.coingecko.com/en/api/documentation
- **API Pricing**: https://www.coingecko.com/en/api/pricing

